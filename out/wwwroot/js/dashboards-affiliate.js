document.addEventListener('DOMContentLoaded', function () {
  // Initialize date range picker
  if ($.fn.daterangepicker) {
    var start = moment().subtract(29, 'days');
    var end = moment();

    $('#daterange-picker').daterangepicker({
      startDate: start,
      endDate: end,
      ranges: {
        'Today': [moment(), moment()],
        'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
        'Last 7 Days': [moment().subtract(6, 'days'), moment()],
        'Last 30 Days': [moment().subtract(29, 'days'), moment()],
        'This Month': [moment().startOf('month'), moment().endOf('month')],
        'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
      }
    }, updateDashboard);

    // Set initial text
    updateDashboard(start, end);
  } else {
    console.error("Date range picker not available. Make sure jQuery and daterangepicker are loaded.");
    // Fallback to default date range
    var start = moment().subtract(29, 'days');
    var end = moment();
    updateDashboard(start, end);
  }

  // Function to update dashboard with date range
  function updateDashboard(start, end) {
    // Update the date range display if it exists
    if ($('#daterange-picker').length) {
      $('#daterange-picker span').html(start.format('MMMM D, YYYY') + ' - ' + end.format('MMMM D, YYYY'));
    }

    // Format dates for API
    const startDate = start.format('YYYY-MM-DD');
    const endDate = end.format('YYYY-MM-DD');

    console.log(`Fetching dashboard data for period: ${startDate} to ${endDate}`);

    // Show loading indicators
    showLoading();

    // Fetch data from API
    fetch(`/api/dashboard/summary?startDate=${startDate}&endDate=${endDate}`)
      .then(response => {
        if (!response.ok) {
          throw new Error(`HTTP error! Status: ${response.status}`);
        }
        return response.json();
      })
      .then(data => {
        console.log("Dashboard data received:", data);

        // Update dashboard components
        updateRevenueStats(data);
        updateCharts(data);
        updateTables(data);

        // Hide loading indicators
        hideLoading();
      })
      .catch(error => {
        console.error('Error fetching dashboard data:', error);

        // Hide loading indicators and show error
        hideLoading();
        showError('Failed to load dashboard data. Please try again later.');
      });
  }

  // Update revenue statistics 
  function updateRevenueStats(data) {
    // Format numbers with commas and currency
    const formatCurrency = (value) => '$' + parseFloat(value).toFixed(2).replace(/\d(?=(\d{3})+\.)/g, '$&,');
    const formatPercent = (value) => parseFloat(value).toFixed(1) + '%';

    // Update revenue numbers
    if ($('#totalRevenue').length) $('#totalRevenue').text(formatCurrency(data.totalRevenue));
    if ($('#affiliateEarnings').length) $('#affiliateEarnings').text(formatCurrency(data.affiliateEarnings));
    if ($('#netProfit').length) $('#netProfit').text(formatCurrency(data.netProfit));

    // Update growth indicators
    if ($('#revenueGrowth').length) {
      const revenueGrowthElement = $('#revenueGrowth');
      revenueGrowthElement.text(formatPercent(data.revenueGrowth));
      revenueGrowthElement.removeClass('text-success text-danger');
      revenueGrowthElement.addClass(data.revenueGrowth >= 0 ? 'text-success' : 'text-danger');
    }
  }

  // Update all charts
  function updateCharts(data) {
    if (window.revenueChart && data.revenueChart) {
      updateRevenueChart(data.revenueChart);
    }

    if (window.affiliateGrowthChart && data.affiliateGrowthChart) {
      updateGrowthChart('affiliateGrowthChart', data.affiliateGrowthChart, '#6610f2');
    }

    if (window.customerGrowthChart && data.customerGrowthChart) {
      updateGrowthChart('customerGrowthChart', data.customerGrowthChart, '#39da8a');
    }
  }

  // Update revenue chart with new data
  function updateRevenueChart(chartData) {
    if (!window.revenueChart) return;

    window.revenueChart.updateOptions({
      xaxis: {
        categories: chartData.dates
      }
    });

    window.revenueChart.updateSeries([
      {
        name: 'Total Revenue',
        data: chartData.totalRevenue
      },
      {
        name: 'Affiliate Earnings',
        data: chartData.affiliateEarnings
      },
      {
        name: 'Net Profit',
        data: chartData.netProfit
      }
    ]);
  }

  // Update growth sparkline charts
  function updateGrowthChart(chartId, data, color) {
    const chart = window[chartId];
    if (!chart) return;

    chart.updateSeries([{
      data: data
    }]);
  }

  // Update data tables
  function updateTables(data) {
    // Update top products table
    if (data.topProducts && data.topProducts.length) {
      const topProductsTable = $('#topProductsTable tbody');
      if (topProductsTable.length) {
        topProductsTable.empty();

        data.topProducts.forEach(product => {
          const growthClass = product.growth >= 0 ? 'text-success' : 'text-danger';
          const growthIcon = product.growth >= 0 ? 'ti-chevron-up' : 'ti-chevron-down';

          topProductsTable.append(`
            <tr>
              <td>${product.name}</td>
              <td>${product.quantity}</td>
              <td>$${product.revenue.toFixed(2)}</td>
              <td class="${growthClass}">
                <i class="${growthIcon}"></i> ${Math.abs(product.growth).toFixed(1)}%
              </td>
            </tr>
          `);
        });
      }
    }

    // Update top affiliates table
    if (data.topAffiliates && data.topAffiliates.length) {
      const topAffiliatesTable = $('#topAffiliatesTable tbody');
      if (topAffiliatesTable.length) {
        topAffiliatesTable.empty();

        data.topAffiliates.forEach(affiliate => {
          const growthClass = affiliate.growth >= 0 ? 'text-success' : 'text-danger';
          const growthIcon = affiliate.growth >= 0 ? 'ti-chevron-up' : 'ti-chevron-down';

          topAffiliatesTable.append(`
            <tr>
              <td>${affiliate.name}</td>
              <td>${affiliate.customers}</td>
              <td>$${affiliate.commission.toFixed(2)}</td>
              <td class="${growthClass}">
                <i class="${growthIcon}"></i> ${Math.abs(affiliate.growth).toFixed(1)}%
              </td>
            </tr>
          `);
        });
      }
    }
  }

  // Helper functions for loading states
  function showLoading() {
    // Add a loading overlay or spinner to charts and tables
    $('.dashboard-chart').addClass('chart-loading');
    $('.dashboard-table').addClass('table-loading');
  }

  function hideLoading() {
    $('.dashboard-chart').removeClass('chart-loading');
    $('.dashboard-table').removeClass('table-loading');
  }

  function showError(message) {
    // Show error message to the user
    const errorAlert = `<div class="alert alert-danger mt-3">${message}</div>`;
    $('#dashboard-alerts').html(errorAlert);

    // Auto-hide after 5 seconds
    setTimeout(() => {
      $('#dashboard-alerts').html('');
    }, 5000);
  }

  // Initialize charts if they don't exist
  function initializeCharts() {
    // Revenue chart
    if ($('#revenueChart').length && typeof ApexCharts !== 'undefined' && !window.revenueChart) {
      const options = {
        chart: {
          height: 300,
          type: 'line',
          toolbar: {
            show: true
          }
        },
        series: [{
          name: 'Total Revenue',
          data: []
        }, {
          name: 'Affiliate Earnings',
          data: []
        }, {
          name: 'Net Profit',
          data: []
        }],
        xaxis: {
          categories: []
        },
        colors: ['#5a8dee', '#ff5b5b', '#39da8a'],
        legend: {
          position: 'top'
        }
      };

      window.revenueChart = new ApexCharts(document.querySelector('#revenueChart'), options);
      window.revenueChart.render();
    }

    // Initialize sparkline charts for growth
    ['affiliateGrowthChart', 'customerGrowthChart'].forEach((id, index) => {
      if ($(`#${id}`).length && typeof ApexCharts !== 'undefined' && !window[id]) {
        const color = index === 0 ? '#6610f2' : '#39da8a';
        const options = {
          chart: {
            type: 'line',
            height: 35,
            sparkline: {
              enabled: true
            },
            animations: {
              enabled: true,
              easing: 'linear',
              speed: 50
            }
          },
          series: [{
            data: [0, 0, 0, 0, 0, 0, 0]
          }],
          stroke: {
            curve: 'smooth',
            width: 2
          },
          colors: [color],
          tooltip: {
            fixed: {
              enabled: false
            },
            x: {
              show: false
            },
            y: {
              formatter: function (value) {
                return value + '%';
              }
            }
          }
        };

        window[id] = new ApexCharts(document.querySelector(`#${id}`), options);
        window[id].render();
      }
    });
  }

  // Call chart initialization on page load
  initializeCharts();
  // Initialize date range picker
  $('#reportrange').daterangepicker({
    startDate: moment().subtract(29, 'days'),
    endDate: moment(),
    ranges: {
      'Today': [moment(), moment()],
      'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
      'Last 7 Days': [moment().subtract(6, 'days'), moment()],
      'Last 30 Days': [moment().subtract(29, 'days'), moment()],
      'This Month': [moment().startOf('month'), moment().endOf('month')],
      'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
    }
  }, function (start, end, label) {
    // When date range changes, update all dashboard data
    $('#reportrange span').html(start.format('MMMM D, YYYY') + ' - ' + end.format('MMMM D, YYYY'));
    loadDashboardData(start.format('YYYY-MM-DD'), end.format('YYYY-MM-DD'));
  });

  $('#reportrange span').html(moment().subtract(29, 'days').format('MMMM D, YYYY') + ' - ' + moment().format('MMMM D, YYYY'));

  // Initial data load
  loadDashboardData(
    moment().subtract(29, 'days').format('YYYY-MM-DD'),
    moment().format('YYYY-MM-DD')
  );

  // Initialize DataTable for orders
  $('.datatables-orders').DataTable({
    dom: '<"card-header d-flex flex-wrap pb-2"<"dt-action-buttons text-xl-end text-lg-start text-md-end text-start"B>><"d-flex justify-content-between align-items-center mx-0 row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>t<"d-flex justify-content-between row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
    displayLength: 7,
    lengthMenu: [7, 10, 25, 50, 75, 100],
    language: {
      search: "",
      searchPlaceholder: "Search Orders"
    },
    // Further DataTable options like AJAX source would be implemented based on your API
  });

  // Export button functionality
  $('#exportOrders').on('click', function () {
    window.location.href = '/Orders/Export?startDate=' +
      $('#reportrange').data('daterangepicker').startDate.format('YYYY-MM-DD') +
      '&endDate=' + $('#reportrange').data('daterangepicker').endDate.format('YYYY-MM-DD');
  });
});

// Function to load all dashboard data based on date range
function loadDashboardData(startDate, endDate) {
  // This would make AJAX calls to your backend API to get the required data
  // For example:
  $.ajax({
    url: '/api/dashboard/summary',
    type: 'GET',
    data: {
      startDate: startDate,
      endDate: endDate
    },
    success: function (data) {
      updateDashboardStats(data);
      initializeCharts(data);
      updateTopAffiliates(data.topAffiliates);
      updateTopProducts(data.topProducts);
    },
    error: function (xhr, status, error) {
      console.error("Error loading dashboard data:", error);
      // Show error message to user
      toastr.error('Failed to load dashboard data. Please try again later.');
    }
  });

  // We might need to make separate calls for specific sections like orders
  refreshOrdersTable(startDate, endDate);
}

// Function to update all dashboard statistics
function updateDashboardStats(data) {
  // Update revenue figures
  $('#totalRevenue').text('$' + formatNumber(data.totalRevenue));
  $('#affiliateEarnings').text('$' + formatNumber(data.affiliateEarnings));
  $('#netProfit').text('$' + formatNumber(data.netProfit));

  // Update key metrics
  $('#activeAffiliatesCount').text(data.activeAffiliates);
  $('#totalCustomersCount').text(data.totalCustomers);
  $('#totalOrdersCount').text(data.totalOrders);
  $('#conversionRate').text(data.conversionRate + '%');

  // Update growth indicators
  updateGrowthIndicator('#affiliateGrowth', data.affiliateGrowth);
  updateGrowthIndicator('#customerGrowth', data.customerGrowth);
  updateGrowthIndicator('#orderGrowth', data.orderGrowth);
  updateGrowthIndicator('#conversionGrowth', data.conversionGrowth);

  // Update profit margins
  $('#avgProfitMargin').text(data.avgProfitMargin + '%');
  $('#bestProfitMargin').text(data.bestProfitMargin + '%');
  $('#lowestProfitMargin').text(data.lowestProfitMargin + '%');

  // Update acquisition channels
  $('#affiliateReferralPercentage').text(data.acquisitionChannels.affiliate + '%');
  $('#organicSearchPercentage').text(data.acquisitionChannels.organic + '%');
  $('#socialMediaPercentage').text(data.acquisitionChannels.social + '%');
  $('#emailMarketingPercentage').text(data.acquisitionChannels.email + '%');

  // Update invoice statistics
  $('#paidInvoicesCount').text(data.invoices.paid.count);
  $('#paidInvoicesAmount').text('$' + formatNumber(data.invoices.paid.amount));
  $('#pendingInvoicesCount').text(data.invoices.pending.count);
  $('#pendingInvoicesAmount').text('$' + formatNumber(data.invoices.pending.amount));
  $('#overdueInvoicesCount').text(data.invoices.overdue.count);
  $('#overdueInvoicesAmount').text('$' + formatNumber(data.invoices.overdue.amount));
  $('#avgPaymentTime').text(data.invoices.avgPaymentDays + ' days');

  // Update commission data
  $('#totalCommissionAmount').text('$' + formatNumber(data.commissions.total));
  $('#paidCommissionAmount').text('$' + formatNumber(data.commissions.paid));
  $('#pendingPayoutAmount').text('$' + formatNumber(data.commissions.pending));
  $('#nextPayoutDate').text(formatDate(data.commissions.nextPayoutDate));
}

// Helper function to update growth indicators
function updateGrowthIndicator(selector, growthValue) {
  const element = $(selector);
  if (growthValue > 0) {
    element.removeClass('bg-label-danger').addClass('bg-label-success');
    element.html('<i class="ti ti-chevron-up"></i> +' + growthValue + '%');
  } else {
    element.removeClass('bg-label-success').addClass('bg-label-danger');
    element.html('<i class="ti ti-chevron-down"></i> ' + growthValue + '%');
  }
}

// Helper function to format numbers with commas
function formatNumber(number) {
  return parseFloat(number).toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

// Helper function to format dates
function formatDate(dateString) {
  if (!dateString) return '--/--/----';
  const date = new Date(dateString);
  return date.toLocaleDateString();
}

// Function to initialize all charts with data
function initializeCharts(data) {
  // Revenue chart
  const revenueChartOptions = {
    series: [
      {
        name: 'Total Revenue',
        data: data.revenueChart.totalRevenue
      },
      {
        name: 'Affiliate Earnings',
        data: data.revenueChart.affiliateEarnings
      },
      {
        name: 'Net Profit',
        data: data.revenueChart.netProfit
      }
    ],
    chart: {
      height: 350,
      type: 'area',
      toolbar: {
        show: true
      }
    },
    dataLabels: {
      enabled: false
    },
    stroke: {
      curve: 'smooth'
    },
    xaxis: {
      type: 'datetime',
      categories: data.revenueChart.dates
    },
    tooltip: {
      shared: true,
      y: {
        formatter: function (value) {
          return '$' + formatNumber(value);
        }
      }
    },
    colors: ['#696cff', '#03c3ec', '#71dd37']
  };

  if (window.revenueChart) {
    window.revenueChart.updateOptions(revenueChartOptions);
  } else {
    window.revenueChart = new ApexCharts(
      document.querySelector("#revenueChart"),
      revenueChartOptions
    );
    window.revenueChart.render();
  }

  // Affiliate Growth Chart (small spark line)
  const affiliateGrowthOptions = {
    series: [{
      data: data.affiliateGrowthChart
    }],
    chart: {
      type: 'line',
      width: 100,
      height: 40,
      sparkline: {
        enabled: true
      }
    },
    colors: ['#696cff'],
    stroke: {
      width: 2
    },
    tooltip: {
      fixed: {
        enabled: false
      },
      x: {
        show: false
      },
      marker: {
        show: false
      }
    }
  };

  if (window.affiliateGrowthChart) {
    window.affiliateGrowthChart.updateOptions(affiliateGrowthOptions);
  } else {
    window.affiliateGrowthChart = new ApexCharts(
      document.querySelector("#affiliateGrowthChart"),
      affiliateGrowthOptions
    );
    window.affiliateGrowthChart.render();
  }

  // Customer Growth Chart (similar to affiliate)
  const customerGrowthOptions = {
    series: [{
      data: data.customerGrowthChart
    }],
    chart: {
      type: 'line',
      width: 100,
      height: 40,
      sparkline: {
        enabled: true
      }
    },
    colors: ['#03c3ec'],
    stroke: {
      width: 2
    },
    tooltip: {
      fixed: {
        enabled: false
      },
      x: {
        show: false
      },
      marker: {
        show: false
      }
    }
  };

  if (window.customerGrowthChart) {
    window.customerGrowthChart.updateOptions(customerGrowthOptions);
  } else {
    window.customerGrowthChart = new ApexCharts(
      document.querySelector("#customerGrowthChart"),
      customerGrowthOptions
    );
    window.customerGrowthChart.render();
  }

  // Top Products Chart
  const topProductsOptions = {
    series: [{
      name: 'Revenue',
      data: data.topProducts.map(p => p.revenue)
    }],
    chart: {
      type: 'bar',
      height: 240,
      toolbar: {
        show: false
      }
    },
    plotOptions: {
      bar: {
        horizontal: false,
        columnWidth: '55%',
        borderRadius: 4
      }
    },
    dataLabels: {
      enabled: false
    },
    xaxis: {
      categories: data.topProducts.map(p => p.name),
      labels: {
        style: {
          fontSize: '12px'
        }
      }
    },
    colors: ['#696cff'],
    tooltip: {
      y: {
        formatter: function (value) {
          return '$' + formatNumber(value);
        }
      }
    }
  };

  if (window.topProductsChart) {
    window.topProductsChart.updateOptions(topProductsOptions);
  } else {
    window.topProductsChart = new ApexCharts(
      document.querySelector("#topProductsChart"),
      topProductsOptions
    );
    window.topProductsChart.render();
  }

  // Profit Margin Chart
  const profitMarginOptions = {
    series: [{
      name: 'Profit Margin',
      data: data.profitMarginChart.margins
    }],
    chart: {
      height: 240,
      type: 'line',
      toolbar: {
        show: false
      }
    },
    stroke: {
      width: 3,
      curve: 'smooth'
    },
    xaxis: {
      categories: data.profitMarginChart.categories,
      labels: {
        style: {
          fontSize: '12px'
        }
      }
    },
    colors: ['#71dd37'],
    markers: {
      size: 4
    },
    tooltip: {
      y: {
        formatter: function (value) {
          return value + '%';
        }
      }
    }
  };

  if (window.profitMarginChart) {
    window.profitMarginChart.updateOptions(profitMarginOptions);
  } else {
    window.profitMarginChart = new ApexCharts(
      document.querySelector("#profitMarginChart"),
      profitMarginOptions
    );
    window.profitMarginChart.render();
  }

  // Acquisition Chart (Donut)
  const acquisitionOptions = {
    series: [
      data.acquisitionChannels.affiliate,
      data.acquisitionChannels.organic,
      data.acquisitionChannels.social,
      data.acquisitionChannels.email
    ],
    chart: {
      type: 'donut',
      height: 240
    },
    labels: ['Affiliate Links', 'Organic Search', 'Social Media', 'Email Marketing'],
    colors: ['#696cff', '#03c3ec', '#71dd37', '#ffab00'],
    plotOptions: {
      pie: {
        donut: {
          size: '70%'
        }
      }
    },
    dataLabels: {
      enabled: false
    },
    legend: {
      show: false
    },
    tooltip: {
      y: {
        formatter: function (value) {
          return value + '%';
        }
      }
    }
  };

  if (window.acquisitionChart) {
    window.acquisitionChart.updateOptions(acquisitionOptions);
  } else {
    window.acquisitionChart = new ApexCharts(
      document.querySelector("#acquisitionChart"),
      acquisitionOptions
    );
    window.acquisitionChart.render();
  }

  // Invoice Status Chart (Donut)
  const invoiceStatusOptions = {
    series: [
      data.invoices.paid.count,
      data.invoices.pending.count,
      data.invoices.overdue.count
    ],
    chart: {
      type: 'donut',
      height: 240
    },
    labels: ['Paid', 'Pending', 'Overdue'],
    colors: ['#71dd37', '#ffab00', '#ff3e1d'],
    plotOptions: {
      pie: {
        donut: {
          size: '70%'
        }
      }
    },
    dataLabels: {
      enabled: false
    },
    legend: {
      show: false
    }
  };

  if (window.invoiceStatusChart) {
    window.invoiceStatusChart.updateOptions(invoiceStatusOptions);
  } else {
    window.invoiceStatusChart = new ApexCharts(
      document.querySelector("#invoiceStatusChart"),
      invoiceStatusOptions
    );
    window.invoiceStatusChart.render();
  }

  // Commission Trend Chart
  const commissionTrendOptions = {
    series: [
      {
        name: 'Total Commissions',
        data: data.commissionTrend.total
      },
      {
        name: 'Paid Commissions',
        data: data.commissionTrend.paid
      }
    ],
    chart: {
      height: 300,
      type: 'line',
      toolbar: {
        show: false
      }
    },
    colors: ['#696cff', '#71dd37'],
    dataLabels: {
      enabled: false
    },
    stroke: {
      curve: 'smooth',
      width: 3
    },
    grid: {
      borderColor: '#f1f1f1'
    },
    xaxis: {
      categories: data.commissionTrend.months
    },
    tooltip: {
      y: {
        formatter: function (value) {
          return '$' + formatNumber(value);
        }
      }
    }
  };

  if (window.commissionTrendChart) {
    window.commissionTrendChart.updateOptions(commissionTrendOptions);
  } else {
    window.commissionTrendChart = new ApexCharts(
      document.querySelector("#commissionTrendChart"),
      commissionTrendOptions
    );
    window.commissionTrendChart.render();
  }
}

// Function to update the top affiliates list
function updateTopAffiliates(affiliates) {
  const container = $('#topAffiliatesList');
  container.empty();

  affiliates.forEach(affiliate => {
    const growthClass = affiliate.growth >= 0 ? 'text-success' : 'text-danger';
    const growthIcon = affiliate.growth >= 0 ? 'ti-chevron-up' : 'ti-chevron-down';

    container.append(`
          <li class="d-flex align-items-center mb-4">
            <div class="avatar flex-shrink-0 me-3">
              ${affiliate.avatar ? `<img src="${affiliate.avatar}" alt="Avatar" class="rounded-circle">` :
        `<span class="avatar-initial rounded-circle bg-label-primary">${affiliate.name.charAt(0)}</span>`}
            </div>
            <div class="d-flex w-100 flex-wrap align-items-center justify-content-between gap-2">
              <div class="me-2">
                <h6 class="mb-0">${affiliate.name}</h6>
                <small class="text-muted">Referred: ${affiliate.customers} customers</small>
              </div>
              <div class="user-progress d-flex align-items-center gap-1">
                      <h6 class="mb-0">$${formatNumber(affiliate.commission)}</h6>
                    <span class="${growthClass} fw-medium">
                      <i class="ti ${growthIcon}"></i> ${Math.abs(affiliate.growth)}%
                    </span>
                  </div>
                </div>
              </li>
            `);
  });
}

// Function to update the top products table
function updateTopProducts(products) {
  const container = $('#topProductsTable');
  container.empty();

  products.forEach(product => {
    const growthClass = product.growth >= 0 ? 'text-success' : 'text-danger';
    const growthIcon = product.growth >= 0 ? 'ti-chevron-up' : 'ti-chevron-down';

    container.append(`
              <tr>
                <td class="py-2">
                  <div class="d-flex align-items-center">
                    <div class="avatar avatar-sm me-2 bg-label-primary">
                      ${product.image ? `<img src="${product.image}" alt="Product" class="rounded-circle">` :
        `<span class="avatar-initial rounded-circle">${product.name.charAt(0)}</span>`}
                    </div>
                    <span>${product.name}</span>
                  </div>
                </td>
                <td class="text-end">$${formatNumber(product.revenue)}</td>
                <td class="text-end">
                  <span class="badge bg-label-${product.growth >= 0 ? 'success' : 'danger'}">
                    <i class="ti ${growthIcon}"></i> ${Math.abs(product.growth)}%
                  </span>
                </td>
              </tr>
            `);
  });
}

// Function to refresh the orders table with new data
function refreshOrdersTable(startDate, endDate) {
  const table = $('.datatables-orders').DataTable();

  // Clear existing data
  table.clear();

  // Fetch new data from API
  $.ajax({
    url: '/api/orders',
    type: 'GET',
    data: {
      startDate: startDate,
      endDate: endDate
    },
    success: function (data) {
      // Add new data to the table
      data.forEach(order => {
        table.row.add([
          `<a href="/Orders/Details/${order.id}">#${order.id}</a>`,
          `<div class="d-flex align-items-center">
                    <div class="avatar avatar-xs me-2">
                      ${order.customerAvatar ? `<img src="${order.customerAvatar}" alt="Avatar" class="rounded-circle">` :
            `<span class="avatar-initial rounded-circle bg-label-info">${order.customerName.charAt(0)}</span>`}
                    </div>
                    <span>${order.customerName}</span>
                  </div>`,
          order.affiliateName || 'Direct',
          formatDate(order.date),
          order.productCount,
          `$${formatNumber(order.amount)}`,
          `$${formatNumber(order.commission)}`,
          `<span class="badge bg-label-${getStatusColor(order.status)}">${order.status}</span>`,
          `<div class="dropdown">
                    <button type="button" class="btn p-0 dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                      <i class="ti ti-dots-vertical"></i>
                    </button>
                    <div class="dropdown-menu">
                      <a class="dropdown-item" href="/Orders/Details/${order.id}">
                        <i class="ti ti-eye me-1"></i> View
                      </a>
                      <a class="dropdown-item" href="/Invoices/Generate/${order.id}">
                        <i class="ti ti-file-invoice me-1"></i> Invoice
                      </a>
                    </div>
                  </div>`
        ]);
      });

      // Redraw the table with new data
      table.draw();
    },
    error: function (xhr, status, error) {
      console.error("Error loading orders data:", error);
      toastr.error('Failed to load orders data. Please try again later.');
    }
  });
}

// Helper function to get status color
function getStatusColor(status) {
  switch (status.toLowerCase()) {
    case 'completed':
      return 'success';
    case 'processing':
      return 'info';
    case 'pending':
      return 'warning';
    case 'cancelled':
      return 'danger';
    case 'refunded':
      return 'secondary';
    default:
      return 'primary';
  }
}
