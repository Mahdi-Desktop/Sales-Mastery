/**
 * Affiliate Dashboard
 */

'use strict';

// Affiliate Sales Chart - Affiliate Dashboard
(function () {
  const affiliateSalesChartEl = document.querySelector('#affiliateSalesChart');

  if (affiliateSalesChartEl) {
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
          data: [45, 52, 38, 24, 33, 56, 42, 20, 36, 60, 33, 45]
        },
        {
          name: 'Commissions',
          data: [10, 12, 9, 6, 8, 14, 10, 5, 9, 15, 8, 11]
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

    // Initialize Chart
    const affiliateSalesChart = new ApexCharts(affiliateSalesChartEl, affiliateSalesChartConfig);
    affiliateSalesChart.render();
  }
})();

// Monthly Referrals Chart
(function () {
  const monthlyReferralsChartEl = document.querySelector('#monthlyReferralsChart');

  if (monthlyReferralsChartEl) {
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
          data: [18, 7, 15, 29, 18, 12, 9]
        },
        {
          name: 'Conversions',
          data: [13, 3, 9, 22, 15, 8, 6]
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

    // Initialize Chart
    const monthlyReferralsChart = new ApexCharts(monthlyReferralsChartEl, monthlyReferralsChartConfig);
    monthlyReferralsChart.render();
  }
})();

// Copy affiliate link
// Copy affiliate link - updated to use modern Clipboard API
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

