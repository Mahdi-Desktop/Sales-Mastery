/**
 * Affiliate Commissions
 *//*

'use strict';

// DataTable initialization with export functionality
$(function () {
  let dt_commission_table = $('.datatable-commission');

  // DataTable with buttons
  if (dt_commission_table.length) {
    var dt_commission = dt_commission_table.DataTable({
      dom: '<"card-header d-flex flex-wrap py-3"<"me-5"f><"dt-action-buttons text-xl-end text-lg-start text-md-end text-start"B>><"row"<"col-sm-12"tr>><"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
      buttons: [
        {
          extend: 'collection',
          className: 'btn btn-label-primary dropdown-toggle me-2',
          text: '<i class="ti ti-file-export me-1"></i>Export',
          buttons: [
            {
              extend: 'print',
              text: '<i class="ti ti-printer me-2"></i>Print',
              className: 'dropdown-item',
              exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
            },
            {
              extend: 'csv',
              text: '<i class="ti ti-file-spreadsheet me-2"></i>Csv',
              className: 'dropdown-item',
              exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
            },
            {
              extend: 'excel',
              text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
              className: 'dropdown-item',
              exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
            },
            {
              extend: 'pdf',
              text: '<i class="ti ti-file-description me-2"></i>Pdf',
              className: 'dropdown-item',
              exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
            },
            {
              extend: 'copy',
              text: '<i class="ti ti-copy me-2"></i>Copy',
              className: 'dropdown-item',
              exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
            }
          ]
        }
      ],
      // Customize the length menu
      lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
      // For responsive
      responsive: {
        details: {
          display: $.fn.dataTable.Responsive.display.modal({
            header: function (row) {
              var data = row.data();
              return 'Commission Details';
            }
          }),
          type: 'column',
          renderer: function (api, rowIdx, columns) {
            var data = $.map(columns, function (col, i) {
              return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                ? '<tr data-dt-row="' +
                col.rowIndex +
                '" data-dt-column="' +
                col.columnIndex +
                '">' +
                '<td>' +
                col.title +
                ':' +
                '</td> ' +
                '<td>' +
                col.data +
                '</td>' +
                '</tr>'
                : '';
            }).join('');

            return data ? $('<table class="table"/><tbody />').append(data) : false;
          }
        }
      },
      // Fixed columns
      scrollX: true,
      // Use pagingType for pagination type
      pagingType: 'simple_numbers'
    });
  }

  // Filter form control to default size
  $('.dataTables_filter .form-control').removeClass('form-control-sm');
  $('.dataTables_length .form-select').removeClass('form-select-sm');
});
*/
/**
 * Affiliate Commissions
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // Wait for Firebase to be ready
  if (window.db) {
    initializeCommissionsTable(window.db);
  } else {
    document.addEventListener('firebase-ready', function (e) {
      initializeCommissionsTable(e.detail.db);
    });
  }
});

function initializeCommissionsTable(db) {
  // Get current user ID from user context
  const currentUserId = getCurrentUserId();

  if (!currentUserId) {
    console.error('User ID not found');
    //showError('Authentication error', 'Please log in to view commission data');
    // Instead, find the table element first
    const tableContainer = document.querySelector('.card-datatable') || document.querySelector('.table-responsive');
    if (tableContainer) {
      tableContainer.innerHTML = `
        <div class="alert alert-danger d-flex align-items-center" role="alert">
          <i class="ti ti-alert-circle me-2"></i>
          <div>
            <h6 class="alert-heading mb-1">Authentication error</h6>
            <span>Please log in to view commission data</span>
          </div>
        </div>
      `;
    }
    return;
  }

  // Get affiliate ID (current user ID if affiliate, or selected affiliate ID if admin)
  const affiliateId = isUserAdmin() && window.selectedAffiliateId ?
    window.selectedAffiliateId : currentUserId;

  // DOM Elements
  const commissionsTable = document.querySelector('.datatable-commission');
  const statusFilter = document.getElementById('statusFilter');
  const searchBox = document.getElementById('searchBox');

  // Show loading state
  if (commissionsTable) {
    const loadingRow = document.createElement('tr');
    loadingRow.innerHTML = `
      <td colspan="7" class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading commissions...</span>
        </div>
        <p class="mt-2 mb-0">Loading commission data...</p>
      </td>
    `;

    const tbody = commissionsTable.querySelector('tbody');
    if (tbody) {
      tbody.innerHTML = '';
      tbody.appendChild(loadingRow);
    }
  }

  // Load commissions data
  loadCommissionsData(db, affiliateId)
    .then(commissions => {
      renderCommissionsTable(commissions);

      // Initialize DataTable
      let dt_commission = $('.datatable-commission').DataTable({
        dom: '<"card-header d-flex flex-wrap py-3"<"me-5"f><"dt-action-buttons text-xl-end text-lg-start text-md-end text-start"B>><"row"<"col-sm-12"tr>><"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
        buttons: [
          {
            extend: 'collection',
            className: 'btn btn-label-primary dropdown-toggle me-2',
            text: '<i class="ti ti-file-export me-1"></i>Export',
            buttons: [
              {
                extend: 'print',
                text: '<i class="ti ti-printer me-2"></i>Print',
                className: 'dropdown-item',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
              },
              {
                extend: 'csv',
                text: '<i class="ti ti-file-spreadsheet me-2"></i>Csv',
                className: 'dropdown-item',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
              },
              {
                extend: 'excel',
                text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
                className: 'dropdown-item',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
              },
              {
                extend: 'pdf',
                text: '<i class="ti ti-file-description me-2"></i>Pdf',
                className: 'dropdown-item',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
              },
              {
                extend: 'copy',
                text: '<i class="ti ti-copy me-2"></i>Copy',
                className: 'dropdown-item',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6] }
              }
            ]
          }
        ],
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        responsive: {
          details: {
            display: $.fn.dataTable.Responsive.display.modal({
              header: function (row) {
                return 'Commission Details';
              }
            }),
            type: 'column',
            renderer: function (api, rowIdx, columns) {
              const data = $.map(columns, function (col, i) {
                return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                  ? '<tr data-dt-row="' +
                  col.rowIndex +
                  '" data-dt-column="' +
                  col.columnIndex +
                  '">' +
                  '<td>' +
                  col.title +
                  ':' +
                  '</td> ' +
                  '<td>' +
                  col.data +
                  '</td>' +
                  '</tr>'
                  : '';
              }).join('');

              return data ? $('<table class="table"/><tbody />').append(data) : false;
            }
          }
        },
        scrollX: true,
        pagingType: 'simple_numbers'
      });

      // Apply custom search
      if (searchBox) {
        searchBox.addEventListener('keyup', function () {
          dt_commission.search(this.value).draw();
        });
      }

      // Apply status filter
      if (statusFilter) {
        statusFilter.addEventListener('change', function () {
          dt_commission.column(5).search(this.value).draw();
        });
      }

      // Filter form control to default size
      $('.dataTables_filter .form-control').removeClass('form-control-sm');
      $('.dataTables_length .form-select').removeClass('form-select-sm');
    })
    .catch(error => {
      console.error('Error loading commissions:', error);
      showError('Data Loading Error', 'Failed to load commission data. Please try again.');
    });

  // Load commissions data from Firestore
  async function loadCommissionsData(db, affiliateId) {
    try {
      // Query commissions for this affiliate
      const commissionsSnapshot = await db.collection('commissions')
        .where('AffiliateId', '==', affiliateId)
        .orderBy('CreatedAt', 'desc')
        .get();

      if (commissionsSnapshot.empty) {
        return [];
      }

      const commissions = [];

      // Process each commission
      for (const doc of commissionsSnapshot.docs) {
        const commission = doc.data();
        commission.id = doc.id;

        // Get customer name if available
        if (commission.CustomerId) {
          try {
            const customerDoc = await db.collection('users').doc(commission.CustomerId).get();
            if (customerDoc.exists) {
              const customerData = customerDoc.data();
              commission.customerName = customerData.Name || customerData.Email || 'Customer';
            }
          } catch (error) {
            console.error('Error fetching customer data:', error);
            commission.customerName = 'Customer';
          }
        } else {
          commission.customerName = 'Customer';
        }

        commissions.push(commission);
      }

      return commissions;
    } catch (error) {
      console.error('Error loading commissions data:', error);
      throw error;
    }
  }

  // Render commissions table
  function renderCommissionsTable(commissions) {
    if (!commissionsTable) return;

    const tbody = commissionsTable.querySelector('tbody');
    if (!tbody) return;

    if (commissions.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-center py-4">
            <i class="ti ti-cash text-primary" style="font-size: 3rem;"></i>
            <h3 class="mt-3">No Commissions Found</h3>
            <p class="mb-0">You don't have any commission records yet.</p>
          </td>
        </tr>
      `;
      return;
    }

    // Clear table
    tbody.innerHTML = '';

    // Add commission rows
    commissions.forEach(commission => {
      const row = document.createElement('tr');

      // Format date
      const createdDate = commission.CreatedAt ? commission.CreatedAt.toDate() : new Date();
      const formattedCreatedDate = formatDate(createdDate);

      // Format paid date
      const paidDate = commission.PaidAt ? commission.PaidAt.toDate() : null;
      const formattedPaidDate = paidDate ? formatDate(paidDate) : '-';

      // Create status badge
      let statusBadge = '';
      switch (commission.Status) {
        case 'Paid':
          statusBadge = '<span class="badge bg-label-success">Paid</span>';
          break;
        case 'Pending':
          statusBadge = '<span class="badge bg-label-warning">Pending</span>';
          break;
        case 'Cancelled':
          statusBadge = '<span class="badge bg-label-danger">Cancelled</span>';
          break;
        default:
          statusBadge = `<span class="badge bg-label-secondary">${commission.Status}</span>`;
          break;
      }

      row.innerHTML = `
        <td>${formattedCreatedDate}</td>
        <td>#${commission.OrderId}</td>
        <td>${commission.customerName}</td>
        <td>$${parseFloat(commission.Amount).toFixed(2)}</td>
        <td>${commission.Rate}%</td>
        <td>${statusBadge}</td>
        <td>${formattedPaidDate}</td>
      `;

      tbody.appendChild(row);
    });
  }

  // Show error message
  function showError(title, message) {
    const tableElement = document.querySelector('.datatable-commission');
    if (!tableElement) {
      console.error(`Error: ${title} - ${message}`);
      return;
    }

    const tbody = tableElement.querySelector('tbody');
    if (!tbody) {
      tableElement.innerHTML = `
      <div class="alert alert-danger d-flex align-items-center" role="alert">
        <i class="ti ti-alert-circle me-2"></i>
        <div>
          <h6 class="alert-heading mb-1">${title}</h6>
          <span>${message}</span>
        </div>
      </div>
    `;
      return;
    }

    tbody.innerHTML = `
    <tr>
      <td colspan="7" class="text-center py-4">
        <div class="alert alert-danger d-flex align-items-center" role="alert">
          <i class="ti ti-alert-circle me-2"></i>
          <div>
            <h6 class="alert-heading mb-1">${title}</h6>
            <span>${message}</span>
          </div>
        </div>
      </td>
    </tr>
  `;
  }

  // Helper function to format date
  function formatDate(date) {
    if (!date) return '-';

    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }
}
