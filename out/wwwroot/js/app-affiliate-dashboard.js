/**
 * Affiliate Dashboard
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // Wait for Firebase to be ready
  if (window.db) {
    initializeDashboard(window.db);
  } else {
    document.addEventListener('firebase-ready', function (e) {
      initializeDashboard(e.detail.db);
    });
  }
});

function initializeDashboard(db) {
  // Check access control
  if (!checkAccessControl()) return;

  // Get current user ID from user context
  const currentUserId = getCurrentUserId();

  // Get affiliate ID (current user ID if affiliate, or selected affiliate ID if admin)
  const affiliateId = isUserAdmin() && window.selectedAffiliateId ?
    window.selectedAffiliateId : currentUserId;

  // Initialize date range picker
  if (document.getElementById('flatpickr-range')) {
    const rangePickr = document.getElementById('flatpickr-range');

    // Set default date range (last 30 days)
    const today = new Date();
    const thirtyDaysAgo = new Date(today);
    thirtyDaysAgo.setDate(today.getDate() - 30);

    flatpickr(rangePickr, {
      mode: 'range',
      defaultDate: [thirtyDaysAgo, today],
      onChange: function (selectedDates) {
        if (selectedDates.length === 2) {
          // Date range selected, refresh dashboard data
          loadDashboardData(db, affiliateId, selectedDates[0], selectedDates[1]);
        }
      }
    });

    // Initial load with default date range
    loadDashboardData(db, affiliateId, thirtyDaysAgo, today);
  }

  // If user is admin, show affiliate selector
  if (isUserAdmin()) {
    const affiliateSelector = document.getElementById('affiliateSelector');
    if (affiliateSelector) {
      affiliateSelector.classList.remove('d-none');

      // Load affiliates for dropdown
      loadAffiliatesForSelector(db, affiliateSelector);
    }
  }

  // Refresh dashboard button
  const refreshButton = document.getElementById('refresh-dashboard');
  if (refreshButton) {
    refreshButton.addEventListener('click', function () {
      const dateRange = document.getElementById('flatpickr-range')._flatpickr.selectedDates;
      if (dateRange.length === 2) {
        loadDashboardData(db, affiliateId, dateRange[0], dateRange[1]);
      }
    });
  }

  // Export dashboard data
  const exportButton = document.getElementById('export-dashboard');
  if (exportButton) {
    exportButton.addEventListener('click', function () {
      exportDashboardData(db, affiliateId);
    });
  }

  // Print dashboard
  const printButton = document.getElementById('print-dashboard');
  if (printButton) {
    printButton.addEventListener('click', function () {
      window.print();
    });
  }
}

// Load affiliates for admin selector
async function loadAffiliatesForSelector(db, selectorElement) {
  try {
    const affiliatesSnapshot = await db.collection('affiliates')
      .where('Status', '==', 'Active')
      .get();

    let html = `
      <select id="adminAffiliateSelector" class="form-select">
        <option value="">All Affiliates</option>
    `;

    for (const doc of affiliatesSnapshot.docs) {
      const affiliate = doc.data();
      const userId = affiliate.UserId;

      // Get user details
      const userDoc = await db.collection('users').doc(userId).get();
      const userData = userDoc.data();

      html += `<option value="${doc.id}">${userData.Name || userData.Email}</option>`;
    }

    html += `</select>`;
    selectorElement.innerHTML = html;

    // Add event listener to selector
    document.getElementById('adminAffiliateSelector').addEventListener('change', function () {
      window.selectedAffiliateId = this.value;

      // Reload dashboard with selected affiliate
      const dateRange = document.getElementById('flatpickr-range')._flatpickr.selectedDates;
      if (dateRange.length === 2) {
        loadDashboardData(db, window.selectedAffiliateId || 'all', dateRange[0], dateRange[1]);
      }
    });
  } catch (error) {
    console.error('Error loading affiliates:', error);
  }
}

// Load dashboard data from Firestore
async function loadDashboardData(db, affiliateId, startDate, endDate) {
  try {
    // Show loading indicators
    document.querySelectorAll('.card-info h5').forEach(el => {
      el.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>';
    });

    // Convert dates to timestamps for Firestore queries
    const startTimestamp = firebase.firestore.Timestamp.fromDate(startDate);
    const endTimestamp = firebase.firestore.Timestamp.fromDate(endDate);

    // Get affiliate data
    let affiliateData;
    if (affiliateId !== 'all') {
      const affiliateDoc = await db.collection('affiliates').doc(affiliateId).get();
      if (affiliateDoc.exists) {
        affiliateData = affiliateDoc.data();
      } else {
        throw new Error('Affiliate not found');
      }
    }

    // Query customers
    let customersQuery = db.collection('users')
      .where('Role', '==', 3) // Role 3 is customer
      .where('CreatedAt', '>=', startTimestamp)
      .where('CreatedAt', '<=', endTimestamp);

    if (affiliateId !== 'all') {
      customersQuery = customersQuery.where('AffiliateId', '==', affiliateId);
    }

    const customersSnapshot = await customersQuery.get();
    const totalCustomers = customersSnapshot.size;

    // Query orders
    let ordersQuery = db.collection('orders')
      .where('CreatedAt', '>=', startTimestamp)
      .where('CreatedAt', '<=', endTimestamp);

    if (affiliateId !== 'all') {
      ordersQuery = ordersQuery.where('AffiliateId', '==', affiliateId);
    }

    const ordersSnapshot = await ordersQuery.get();

    let totalOrders = 0;
    let totalRevenue = 0;
    let totalCommissions = 0;

    ordersSnapshot.forEach(doc => {
      const order = doc.data();
      totalOrders++;
      totalRevenue += parseFloat(order.Total || 0);
      totalCommissions += parseFloat(order.Commission || 0);
    });

    // Update dashboard stats
    document.getElementById('total-customers').textContent = totalCustomers;
    document.getElementById('total-orders').textContent = totalOrders;
    document.getElementById('total-revenue').textContent = '$' + totalRevenue.toFixed(2);
    document.getElementById('total-commissions').textContent = '$' + totalCommissions.toFixed(2);

    // Calculate growth percentages (would need previous period data for real implementation)
    // For now, using placeholder values
    document.getElementById('customers-growth').textContent = '+12%';
    document.getElementById('customers-progress').style.width = '12%';

    document.getElementById('orders-growth').textContent = '+8%';
    document.getElementById('orders-progress').style.width = '8%';

    document.getElementById('revenue-growth').textContent = '+15%';
    document.getElementById('revenue-progress').style.width = '15%';

    document.getElementById('commissions-growth').textContent = '+18%';
    document.getElementById('commissions-progress').style.width = '18%';

    // Update charts with real data
    updateSalesChart(ordersSnapshot.docs);
    updateReferralsChart(customersSnapshot.docs);

    // Update affiliate link if applicable
    if (affiliateId !== 'all' && affiliateData) {
      const affiliateLinkInput = document.getElementById('affiliateLink');
      if (affiliateLinkInput) {
        affiliateLinkInput.value = `https://yourdomain.com/ref/${affiliateData.AffiliateCode}`;
      }
    }
  } catch (error) {
    console.error('Error loading dashboard data:', error);

    // Show error message
    Swal.fire({
      title: 'Error!',
  text: 'Failed to load dashboard data. Please try again.',
    icon: 'error',
      customClass: {
  confirmButton: 'btn btn-primary'
},
buttonsStyling: false
    });
  }
}

// Update sales chart with real data
function updateSalesChart(orderDocs) {
  const salesChartEl = document.querySelector('#affiliateSalesChart');
  if (!salesChartEl) return;

  // Process order data by month
  const salesByMonth = Array(12).fill(0);
  const commissionsByMonth = Array(12).fill(0);

  orderDocs.forEach(doc => {
    const order = doc.data();
    const orderDate = order.CreatedAt.toDate();
    const month = orderDate.getMonth();

    salesByMonth[month] += parseFloat(order.Total || 0);
    commissionsByMonth[month] += parseFloat(order.Commission || 0);
  });

  // Round to 2 decimal places
  salesByMonth.forEach((val, i) => {
    salesByMonth[i] = parseFloat(val.toFixed(2));
  });

  commissionsByMonth.forEach((val, i) => {
    commissionsByMonth[i] = parseFloat(val.toFixed(2));
  });

  // Update chart
  if (window.affiliateSalesChart) {
    window.affiliateSalesChart.updateSeries([
      {
        name: 'Sales',
        data: salesByMonth
      },
      {
        name: 'Commissions',
        data: commissionsByMonth
      }
    ]);
  } else {
    // Initialize chart if not already done
    const affiliateSalesChartConfig = {
      chart: {
        height: 300,
        type: 'line',
        toolbar: {
          show: false
        }
      },
      series: [
        {
          name: 'Sales',
          data: salesByMonth
        },
        {
          name: 'Commissions',
          data: commissionsByMonth
        }
      ],
      xaxis: {
        categories: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
        labels: {
          style: {
            fontSize: '13px',
            colors: '#697a8d',
            fontFamily: 'Public Sans'
          }
        },
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        }
      },
      yaxis: {
        labels: {
          style: {
            fontSize: '13px',
            colors: '#697a8d',
            fontFamily: 'Public Sans'
          },
          formatter: function (val) {
            return '$' + val;
          }
        }
      },
      colors: [config.colors.primary, config.colors.warning],
      legend: {
        show: true,
        position: 'top',
        horizontalAlign: 'left'
      },
      grid: {
        borderColor: '#f0f0f0',
        padding: {
          top: 0,
          bottom: -8,
          left: 20,
          right: 20
        }
      },
      markers: {
        size: 4,
        colors: config.colors.white,
        strokeColors: [config.colors.primary, config.colors.warning],
        strokeWidth: 2,
        hover: {
          size: 6
        }
      },
      stroke: {
        curve: 'smooth',
        width: 3
      }
    };

    window.affiliateSalesChart = new ApexCharts(salesChartEl, affiliateSalesChartConfig);
    window.affiliateSalesChart.render();
  }
}

// Update referrals chart with real data
function updateReferralsChart(customerDocs) {
  const referralsChartEl = document.querySelector('#monthlyReferralsChart');
  if (!referralsChartEl) return;

  // Process customer data by day of week
  const referralsByDay = Array(7).fill(0);
  const conversionsByDay = Array(7).fill(0);

  customerDocs.forEach(doc => {
    const customer = doc.data();
    const customerDate = customer.CreatedAt.toDate();
    const dayOfWeek = customerDate.getDay(); // 0 = Sunday, 6 = Saturday

    // Adjust to Monday = 0, Sunday = 6
    const adjustedDay = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

    referralsByDay[adjustedDay]++;

    // For conversions, check if customer has orders
    if (customer.OrderCount && customer.OrderCount > 0) {
      conversionsByDay[adjustedDay]++;
    }
  });

  // Update chart
  if (window.monthlyReferralsChart) {
    window.monthlyReferralsChart.updateSeries([
      {
        name: 'Referrals',
        data: referralsByDay
      },
      {
        name: 'Conversions',
        data: conversionsByDay
      }
    ]);
  } else {
    // Initialize chart if not already done
    const monthlyReferralsChartConfig = {
      chart: {
        height: 245,
        type: 'bar',
        toolbar: {
          show: false
        }
      },
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '50%',
          borderRadius: 5,
          startingShape: 'rounded'
        }
      },
      series: [
        {
          name: 'Referrals',
          data: referralsByDay
        },
        {
          name: 'Conversions',
          data: conversionsByDay
        }
      ],
      dataLabels: {
        enabled: false
      },
      colors: [config.colors.primary, config.colors.success],
      xaxis: {
        categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
        axisBorder: {
          show: false
        },
        axisTicks: {
          show: false
        },
        labels: {
          style: {
            fontSize: '13px',
            colors: '#697a8d',
            fontFamily: 'Public Sans'
          }
        }
      },
      yaxis: {
        labels: {
          style: {
            fontSize: '13px',
            colors: '#697a8d',
            fontFamily: 'Public Sans'
          }
        }
      },
      legend: {
        show: true,
        position: 'top',
        horizontalAlign: 'left'
      },
      grid: {
        borderColor: '#f0f0f0',
        padding: {
          top: 0,
          bottom: -8,
          left: 20,
          right: 20
        }
      },
      tooltip: {
        y: {
          formatter: function (val) {
            return val + ' users';
          }
        }
      }
    };

    window.monthlyReferralsChart = new ApexCharts(referralsChartEl, monthlyReferralsChartConfig);
    window.monthlyReferralsChart.render();
  }
}

// Export dashboard data
function exportDashboardData(db, affiliateId) {
  // Get date range
  const dateRange = document.getElementById('flatpickr-range')._flatpickr.selectedDates;
  if (dateRange.length !== 2) {
    Swal.fire({
      title: 'Error!',
      text: 'Please select a date range first.',
      icon: 'error',
      customClass: {
        confirmButton: 'btn btn-primary'
      },
      buttonsStyling: false
    });
    return;
  }

  // Show loading
  Swal.fire({
    title: 'Generating Export',
    html: 'Please wait while we prepare your data...',
    allowOutsideClick: false,
    didOpen: () => {
      Swal.showLoading();
    }
  });

  // In a real application, you would generate a CSV or Excel file
  // For this example, we'll just simulate a delay
  setTimeout(() => {
    Swal.close();

    // Create a simple CSV
    const csvContent = 'data:text/csv;charset=utf-8,' +
      'Metric,Value\n' +
      'Total Customers,' + document.getElementById('total-customers').textContent + '\n' +
      'Total Orders,' + document.getElementById('total-orders').textContent + '\n' +
      'Total Revenue,' + document.getElementById('total-revenue').textContent + '\n' +
      'Total Commissions,' + document.getElementById('total-commissions').textContent;

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', 'affiliate-dashboard-export.csv');
    document.body.appendChild(link);

    link.click();
    document.body.removeChild(link);
  }, 1500);
}

// Copy affiliate link
function copyAffiliateLink() {
  var copyText = document.getElementById("affiliateLink");

  // Use the Clipboard API if available
  if (navigator.clipboard) {
    navigator.clipboard.writeText(copyText.value)
      .then(() => {
        // Show tooltip or notification
        Swal.fire({
          position: 'top-end',
          icon: 'success',
          title: 'Affiliate link copied!',
          showConfirmButton: false,
          timer: 1500,
          customClass: {
            confirmButton: 'btn btn-primary'
          },
          buttonsStyling: false
        });
      })
      .catch(err => {
        console.error('Failed to copy: ', err);
      });
  } else {
    // Fallback for browsers that don't support Clipboard API
    copyText.select();
    try {
      document.execCommand("copy");

      // Show tooltip or notification
      Swal.fire({
        position: 'top-end',
        icon: 'success',
        title: 'Affiliate link copied!',
        showConfirmButton: false,
        timer: 1500,
        customClass: {
          confirmButton: 'btn btn-primary'
        },
        buttonsStyling: false
      });
    } catch (err) {
      console.error('Failed to copy: ', err);
    }
  }
}
