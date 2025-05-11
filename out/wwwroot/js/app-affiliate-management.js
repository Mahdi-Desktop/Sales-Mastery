/**
 * Affiliate Management
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // Check if user is admin
  if (!isUserAdmin()) {
    window.location.href = '/Home/AccessDenied';
    return;
  }

  // Wait for Firebase to be ready
  if (window.db) {
    initializeAffiliateManagement(window.db);
  } else {
    document.addEventListener('firebase-ready', function (e) {
      initializeAffiliateManagement(e.detail.db);
    });
  }
});

function initializeAffiliateManagement(db) {
  // Add the "Add Affiliate" button to the header
  const headerDiv = document.querySelector('.d-flex.justify-content-between.align-items-center.row');
  if (headerDiv) {
    const buttonDiv = document.createElement('div');
    buttonDiv.className = 'col-md-4 text-end';
    buttonDiv.innerHTML = `
      <a href="/Affiliate/Create" class="btn btn-primary">
        <i class="ti ti-plus me-1"></i>Add Affiliate
      </a>
    `;
    headerDiv.appendChild(buttonDiv);
  }

  // Initialize DataTable
  let dt_affiliate_table = $('.datatable-affiliates');

  if (dt_affiliate_table.length) {
    // Show loading state
    dt_affiliate_table.find('tbody').html(`
      <tr>
        <td colspan="9" class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading affiliates...</span>
          </div>
          <p class="mt-2">Loading affiliate data...</p>
        </td>
      </tr>
    `);

    // Load affiliates data
    loadAffiliatesData(db)
      .then(affiliates => {
        // Initialize DataTable with data
        var dt_affiliate = dt_affiliate_table.DataTable({
          data: affiliates,
          columns: [
            { data: '' }, // Empty column for responsive control
            { data: 'name' },
            { data: 'email' },
            { data: 'phone' },
            { data: 'status' },
            { data: 'customers' },
            { data: 'orders' },
            { data: 'revenue' },
            { data: 'actions' }
          ],
          columnDefs: [
            {
              // For Responsive
              className: 'control',
              orderable: false,
              searchable: false,
              responsivePriority: 2,
              targets: 0,
              render: function () {
                return '';
              }
            },
            {
              // Affiliate name and avatar
              targets: 1,
              responsivePriority: 1,
              render: function (data, type, full) {
                const initials = getInitials(full.name);

                return `
                  <div class="d-flex justify-content-start align-items-center">
                    <div class="avatar-wrapper">
                      <div class="avatar avatar-sm me-3">
                        <span class="avatar-initial rounded-circle bg-label-primary">${initials}</span>
                      </div>
                    </div>
                    <div class="d-flex flex-column">
                      <a href="javascript:void(0)" class="text-body fw-medium">${full.name}</a>
                      <small class="text-muted">Since ${full.joinedDate}</small>
                    </div>
                  </div>
                `;
              }
            },
            {
              // Status
              targets: 4,
              render: function (data, type, full) {
                const statusColors = {
                  'Active': 'success',
                  'Inactive': 'secondary',
                  'Pending': 'warning'
                };

                const color = statusColors[full.status] || 'primary';

                return `<span class="badge bg-label-${color}">${full.status}</span>`;
              }
            },
            {
              // Actions column
              targets: -1,
              title: 'Actions',
              searchable: false,
              orderable: false,
              render: function (data, type, full) {
                return `
                  <div class="d-inline-block">
                    <button class="btn btn-sm btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                      <i class="ti ti-dots-vertical"></i>
                    </button>
                    <div class="dropdown-menu dropdown-menu-end">

  <a href="javascript:void(0)" class="dropdown-item" onclick="editAffiliate('${full.id}')">
    <i class="ti ti-pencil me-1"></i> Edit
  </a>
  <a href="javascript:void(0)" class="dropdown-item" onclick="viewCommissions('${full.id}')">
    <i class="ti ti-cash me-1"></i> View Commissions
  </a>
  <a href="javascript:void(0)" class="dropdown-item text-danger" onclick="deleteAffiliate('${full.id}', '${full.name}')">
    <i class="ti ti-trash me-1"></i> Delete
  </a>
</div>
                  </div >
  `;
              }
            }
          ],
          order: [[1, 'asc']],
          dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
          lengthMenu: [10, 25, 50, 75, 100],
          pagingType: 'simple_numbers',
          responsive: {
            details: {
              display: $.fn.dataTable.Responsive.display.modal({
                header: function(row) {
                  const data = row.data();
                  return 'Details of ' + data.name;
                }
              }),
              type: 'column',
              renderer: function(api, rowIdx, columns) {
                const data = $.map(columns, function(col, i) {
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
          language: {
            search: '',
            searchPlaceholder: 'Search affiliates'
          }
        });
        
        // Add modals to the page
        addModalsToPage();
      })
      .catch(error => {
        console.error('Error loading affiliates:', error);
        dt_affiliate_table.find('tbody').html(`
  < tr >
  <td colspan="9" class="text-center py-5">
    <div class="alert alert-danger d-flex align-items-center" role="alert">
      <i class="ti ti-alert-circle me-2"></i>
      <div>
        <h6 class="alert-heading mb-1">Error Loading Data</h6>
        <span>Failed to load affiliate data. Please try again.</span>
      </div>
    </div>
  </td>
          </tr >
  `);
      });
  }
  
  // Filter form control to default size
  $('.dataTables_filter .form-control').removeClass('form-control-sm');
  $('.dataTables_length .form-select').removeClass('form-select-sm');
}

// Load affiliates data from Firestore
async function loadAffiliatesData(db) {
  try {
    // Query all affiliates
    const affiliatesSnapshot = await db.collection('affiliates').get();
    
    if (affiliatesSnapshot.empty) {
      return [];
    }
    
    const affiliates = [];
    
    // Process each affiliate
    for (const doc of affiliatesSnapshot.docs) {
      const affiliate = doc.data();
      affiliate.id = doc.id;
      
      // Get user details
      const userDoc = await db.collection('users').doc(affiliate.UserId).get();
      if (userDoc.exists) {
        const userData = userDoc.data();
        
        // Get customer count
        const customersSnapshot = await db.collection('users')
          .where('Role', '==', 3) // Role 3 is customer
          .where('AffiliateId', '==', doc.id)
          .get();
        
        // Get orders and revenue
        const ordersSnapshot = await db.collection('orders')
          .where('AffiliateId', '==', doc.id)
          .get();
        
        let totalOrders = 0;
        let totalRevenue = 0;
        
        ordersSnapshot.forEach(orderDoc => {
          const order = orderDoc.data();
          totalOrders++;
          totalRevenue += parseFloat(order.Total || 0);
        });
        
        // Format joined date
        const joinedDate = affiliate.CreatedAt ? 
          new Intl.DateTimeFormat('en-US', { month: 'short', year: 'numeric' }).format(affiliate.CreatedAt.toDate()) : 
          'N/A';
        
        // Add to affiliates array in format needed for DataTable
        affiliates.push({
          id: doc.id,
          name: userData.Name || 'Unnamed Affiliate',
          email: userData.Email || 'No Email',
          phone: userData.PhoneNumber || 'No Phone',
          status: affiliate.Status || 'Inactive',
          customers: customersSnapshot.size,
          orders: totalOrders,
          revenue: '$' + totalRevenue.toFixed(2),
          joinedDate: joinedDate,
          actions: '' // Will be rendered by DataTable
        });
      }
    }
    
    return affiliates;
  } catch (error) {
    console.error('Error loading affiliates data:', error);
    throw error;
  }
}

// Edit affiliate details
function editAffiliate(affiliateId) {
  // Show loading state in modal
  $('#editAffiliateModal .modal-body').html(`
  < div class="text-center py-4" >
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <p class="mt-2">Loading affiliate data...</p>
    </div >
  `);
  
  // Show the modal
  $('#editAffiliateModal').modal('show');
  
  // Fetch affiliate data
  const db = window.db;
  if (!db) {
    showModalError('Firebase not initialized');
    return;
  }
  
  db.collection('affiliates').doc(affiliateId).get()
    .then(async doc => {
      if (!doc.exists) {
        showModalError('Affiliate not found');
        return;
      }
      
      const affiliate = doc.data();
      
      // Get user details
      const userDoc = await db.collection('users').doc(affiliate.UserId).get();
      if (!userDoc.exists) {
        showModalError('User data not found');
        return;
      }
      
      const userData = userDoc.data();
      
      // Update modal with form
      $('#editAffiliateModal .modal-body').html(`
  < form id = "editAffiliateForm" >
    <input type="hidden" id="affiliateId" value="${affiliateId}">
      <input type="hidden" id="userId" value="${affiliate.UserId}">

        <div class="row">
          <div class="col-md-6 mb-3">
            <label for="firstName" class="form-label">First Name</label>
            <input type="text" id="firstName" class="form-control" value="${userData.FirstName || ''}" required>
          </div>

          <div class="col-md-6 mb-3">
            <label for="lastName" class="form-label">Last Name</label>
            <input type="text" id="lastName" class="form-control" value="${userData.LastName || ''}" required>
          </div>

          <div class="col-md-6 mb-3">
            <label for="email" class="form-label">Email</label>
            <input type="email" id="email" class="form-control" value="${userData.Email || ''}" required>
          </div>

          <div class="col-md-6 mb-3">
            <label for="phone" class="form-label">Phone</label>
            <input type="text" id="phone" class="form-control" value="${userData.PhoneNumber || ''}">
          </div>

          <div class="col-md-6 mb-3">
            <label for="commissionRate" class="form-label">Commission Rate (%)</label>
            <input type="number" id="commissionRate" class="form-control" min="0" max="100" step="0.5" value="${affiliate.CommissionRate || 15}" required>
          </div>

          <div class="col-md-6 mb-3">
            <label for="status" class="form-label">Status</label>
            <select id="status" class="form-select">
              <option value="Active" ${affiliate.Status === 'Active' ? 'selected' : ''}>Active</option>
              <option value="Inactive" ${affiliate.Status === 'Inactive' ? 'selected' : ''}>Inactive</option>
              <option value="Pending" ${affiliate.Status === 'Pending' ? 'selected' : ''}>Pending</option>
            </select>
          </div>
        </div>
      </form>
      `);

      // Update modal footer
      $('#editAffiliateModal .modal-footer').html(`
      <button type="button" class="btn btn-label-secondary" data-bs-dismiss="modal">Cancel</button>
      <button type="button" class="btn btn-primary" id="saveAffiliateBtn">Save Changes</button>
      `);

      // Add save handler
      $('#saveAffiliateBtn').on('click', function() {
        saveAffiliateChanges(db);
      });
    })
    .catch(error => {
        console.error('Error fetching affiliate data:', error);
      showModalError('Failed to load affiliate data');
    });
}

      // Save affiliate changes
      function saveAffiliateChanges(db) {
        // Show loading state
        $('#saveAffiliateBtn').html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...');
      $('#saveAffiliateBtn').prop('disabled', true);

      // Get form data
      const affiliateId = $('#affiliateId').val();
      const userId = $('#userId').val();
      const firstName = $('#firstName').val();
      const lastName = $('#lastName').val();
      const email = $('#email').val();
      const phone = $('#phone').val();
      const commissionRate = parseFloat($('#commissionRate').val());
      const status = $('#status').val();

      // Update user data
      const userUpdate = db.collection('users').doc(userId).update({
        FirstName: firstName,
      LastName: lastName,
      Email: email,
      PhoneNumber: phone,
      Name: `${firstName} ${lastName}`
  });

      // Update affiliate data
      const affiliateUpdate = db.collection('affiliates').doc(affiliateId).update({
        CommissionRate: commissionRate,
      Status: status
  });

      // Wait for both updates to complete
      Promise.all([userUpdate, affiliateUpdate])
    .then(() => {
        // Close modal
        $('#editAffiliateModal').modal('hide');

      // Show success message
      Swal.fire({
        title: 'Success!',
      text: 'Affiliate information has been updated.',
      icon: 'success',
      customClass: {
        confirmButton: 'btn btn-success'
        },
      buttonsStyling: false
      }).then(() => {
        // Reload the page to refresh the affiliate list
        location.reload();
      });
    })
    .catch(error => {
        console.error('Error updating affiliate:', error);

      // Show error in modal
      $('#saveAffiliateBtn').html('Save Changes');
      $('#saveAffiliateBtn').prop('disabled', false);

      // Show error alert in modal
      $('#editAffiliateForm').prepend(`
      <div class="alert alert-danger" role="alert">
        <i class="ti ti-alert-circle me-1"></i>
        Failed to update affiliate. Please try again.
      </div>
      `);
    });
}

      // Delete affiliate with confirmation
      function deleteAffiliate(affiliateId, affiliateName) {
        Swal.fire({
          title: 'Are you sure?',
          text: `You are about to delete ${affiliateName} from the affiliate program. This action cannot be undone!`,
          icon: 'warning',
          showCancelButton: true,
          confirmButtonText: 'Yes, delete it!',
          cancelButtonText: 'No, cancel',
          customClass: {
            confirmButton: 'btn btn-danger me-3',
            cancelButton: 'btn btn-label-secondary'
          },
          buttonsStyling: false
        }).then(function (result) {
          if (result.isConfirmed) {
            // Show loading
            Swal.fire({
              title: 'Deleting...',
              html: 'Please wait while we process your request.',
              allowOutsideClick: false,
              didOpen: () => {
                Swal.showLoading();
              }
            });

            const db = window.db;
            if (!db) {
              Swal.fire({
                title: 'Error!',
                text: 'Firebase not initialized',
                icon: 'error',
                customClass: {
                  confirmButton: 'btn btn-primary'
                },
                buttonsStyling: false
              });
              return;
            }

            // Get the user ID first
            db.collection('affiliates').doc(affiliateId).get()
              .then(doc => {
                if (!doc.exists) {
                  throw new Error('Affiliate not found');
                }

                const affiliate = doc.data();
                const userId = affiliate.UserId;

                // Delete affiliate document
                return db.collection('affiliates').doc(affiliateId).delete()
                  .then(() => {
                    // Update user role to regular user instead of deleting
                    return db.collection('users').doc(userId).update({
                      Role: 4, // Regular user role
                      IsAffiliate: false
                    });
                  });
              })
              .then(() => {
                Swal.fire({
                  title: 'Deleted!',
                  text: `${affiliateName} has been removed from the affiliate program.`,
                  icon: 'success',
                  customClass: {
                    confirmButton: 'btn btn-success'
                  },
                  buttonsStyling: false
                }).then(() => {
                  // Reload the page to refresh the affiliate list
                  location.reload();
                });
              })
              .catch(error => {
                console.error('Error deleting affiliate:', error);

                Swal.fire({
                  title: 'Error!',
                  text: 'Failed to delete the affiliate. Please try again.',
                  icon: 'error',
                  customClass: {
                    confirmButton: 'btn btn-primary'
                  },
                  buttonsStyling: false
                });
              });
          }
        });
}


// View commissions for a specific affiliate
function viewCommissions(affiliateId) {
  window.location.href = `/Affiliate/Commissions?affiliateId=${affiliateId}`;
}

// Show error in modal
function showModalError(message) {
  $('#editAffiliateModal .modal-body').html(`
    <div class="alert alert-danger d-flex align-items-center" role="alert">
      <i class="ti ti-alert-circle me-2"></i>
      <div>
        <h6 class="alert-heading mb-1">Error</h6>
        <span>${message}</span>
      </div>
    </div>
  `);

  // Update modal footer
  $('#editAffiliateModal .modal-footer').html(`
    <button type="button" class="btn btn-label-secondary" data-bs-dismiss="modal">Close</button>
  `);
}

// Add modals to the page
function addModalsToPage() {
  // Edit Affiliate Modal
  const editModal = document.createElement('div');
  editModal.className = 'modal fade';
  editModal.id = 'editAffiliateModal';
  editModal.tabIndex = '-1';
  editModal.setAttribute('aria-hidden', 'true');

  editModal.innerHTML = `
    <div class="modal-dialog modal-lg modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">Edit Affiliate</h5>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body">
          <!-- Content will be dynamically loaded -->
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-label-secondary" data-bs-dismiss="modal">Cancel</button>
          <button type="button" class="btn btn-primary" id="saveAffiliateBtn">Save Changes</button>
        </div>
      </div>
    </div>
  `;

  document.body.appendChild(editModal);
}

// Helper function to get initials from name
function getInitials(name) {
  if (!name) return 'NA';

  const parts = name.split(' ');
  if (parts.length === 1) {
    return name.charAt(0).toUpperCase();
  }

  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
}
