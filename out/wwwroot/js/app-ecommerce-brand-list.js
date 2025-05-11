// Initialize Firebase for Brands
document.addEventListener('DOMContentLoaded', function () {
  // Firebase configuration is passed from the server via the firebaseConfig variable

  // Initialize Firebase
  if (!firebase.apps.length) {
    firebase.initializeApp(firebaseConfig);
  }

  const db = firebase.firestore();
  const brandsCollection = db.collection('brands');

  let brandsData = []; // To store brands data

  // Configure toastr (Vuexy toast notifications) if not already configured
  if (!toastr.options.closeButton) {
    toastr.options = {
      closeButton: true,
      newestOnTop: true,
      progressBar: true,
      positionClass: 'toast-top-right',
      preventDuplicates: false,
      onclick: null,
      showDuration: '300',
      hideDuration: '1000',
      timeOut: '5000',
      extendedTimeOut: '1000',
      showEasing: 'swing',
      hideEasing: 'linear',
      showMethod: 'fadeIn',
      hideMethod: 'fadeOut'
    };
  }

  // Initialize DataTable for brands
  const brandTable = $('.datatables-brand-list').DataTable({
    processing: true,
    serverSide: false,
    data: [], // Start with empty data
    columns: [
      { data: null, defaultContent: '' }, // Responsive control column
      { data: null, defaultContent: '' }, // Checkbox column
      { data: 'name', defaultContent: '' }, // Brand name
      { data: 'commissionRate', defaultContent: '' }, // Commission rate
      { data: 'createdAt', defaultContent: '' }, // Created date
      { data: null, defaultContent: '' } // Actions
    ],
    columnDefs: [
      {
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
        targets: 1,
        orderable: false,
        searchable: false,
        responsivePriority: 3,
        render: function () {
          return '<input type="checkbox" class="dt-checkboxes form-check-input">';
        },
        checkboxes: {
          selectAllRender: '<input type="checkbox" class="form-check-input">'
        }
      },
      {
        targets: 2,
        responsivePriority: 1,
        render: function (data, type, full) {
          return `<div class="d-flex align-items-center">
                    <div class="avatar-wrapper me-3">
                      <div class="avatar rounded-2 bg-label-secondary">
                        <span class="avatar-initial rounded-2 bg-label-primary">${full.name ? full.name.charAt(0) : 'B'}</span>
                      </div>
                    </div>
                                        <div class="d-flex flex-column">
                      <span class="text-nowrap fw-medium">${full.name || 'Unnamed Brand'}</span>
                    </div>
                  </div>`;
        }
      },
      {
        targets: 3,
        render: function (data, type, full) {
          return `<div class="text-sm-end">
                    <span class="text-nowrap fw-medium">${full.commissionRate !== undefined ? full.commissionRate + '%' : 'N/A'}</span>
                  </div>`;
        }
      },
      {
        targets: 4,
        render: function (data, type, full) {
          // Format date for display
          const formattedDate = full.createdAt ? moment(full.createdAt).format('MMM DD, YYYY') : 'N/A';
          return `<div class="text-sm-end">
                    <span class="text-nowrap">${formattedDate}</span>
                  </div>`;
        }
      },
      {
        targets: -1,
        title: 'Actions',
        orderable: false,
        searchable: false,
        render: function (data, type, full) {
          return `<div class="d-flex align-items-center">
                    <a href="javascript:;" class="text-body edit-brand" data-id="${full.id}"><i class="ti ti-edit ti-sm me-2"></i></a>
                    <a href="javascript:;" class="text-body delete-brand" data-id="${full.id}"><i class="ti ti-trash ti-sm mx-2"></i></a>
                  </div>`;
        }
      }
    ],
    order: [[2, 'asc']],
    dom: '<"card-header d-flex flex-column flex-md-row align-items-md-center py-md-0"<"me-auto"<"d-flex align-items-center"f<"dt-action-buttons ms-3"B>>>><"row"<"col-12"tr>><"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
    lengthMenu: [7, 10, 25, 50, 75, 100],
    buttons: [
      {
        text: '<i class="ti ti-plus me-0 me-sm-1 ti-xs"></i><span class="d-none d-sm-inline-block">Add Brand</span>',
        className: 'btn btn-primary',
        attr: {
          'data-bs-toggle': 'offcanvas',
          'data-bs-target': '#offcanvasEcommerceBrandList'
        }
      }
    ],
    responsive: {
      details: {
        display: $.fn.dataTable.Responsive.display.modal({
          header: function (row) {
            var data = row.data();
            return 'Details of ' + (data.name || 'Brand');
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
    language: {
      search: '',
      searchPlaceholder: 'Search brands...',
      paginate: {
        previous: '<i class="ti ti-chevron-left ti-xs"></i>',
        next: '<i class="ti ti-chevron-right ti-xs"></i>'
      },
      info: 'Showing _START_ to _END_ of _TOTAL_ entries',
      lengthMenu: 'Show _MENU_ entries',
      infoEmpty: 'No records found',
      zeroRecords: 'No matching records found'
    },
    drawCallback: function () {
      // Check if there are no records
      if ($(this).find('tbody tr').length === 0) {
        // If no records found after search
        if ($(this).DataTable().search() !== '') {
          // Show toast notification instead of error
          toastr.info('No matching brands found. Try a different search term.', 'No Results');
        }
      }
    }
  });

  // Add event listener for search to clear toast when user starts a new search
  $('.dataTables_filter input').on('keyup', function () {
    // Clear any existing toasts when user starts typing a new search
    toastr.clear();
  });

  // Fetch brands from Firestore
  function fetchBrands() {
    brandsData = [];
    brandsCollection.get().then((querySnapshot) => {
      querySnapshot.forEach((doc) => {
        const data = doc.data();
        brandsData.push({
          id: doc.id,
          name: data.Name || 'Unnamed Brand',
          commissionRate: data.CommissionRate || 0,
          createdAt: data.CreatedAt ? data.CreatedAt.toDate() : new Date(),
          updatedAt: data.UpdatedAt ? data.UpdatedAt.toDate() : new Date()
        });
      });

      brandTable.clear().rows.add(brandsData).draw();

      // Show message if no brands exist
      if (brandsData.length === 0) {
        toastr.info('No brands found. Click "Add Brand" to create one.', 'No Brands');
      }
    }).catch((error) => {
      console.error("Error getting brands: ", error);
      toastr.error('Failed to load brands', 'Error');
    });
  }

  // Form validation and submission
  const brandForm = document.getElementById('eCommerceBrandListForm');
  const brandFormValidation = FormValidation.formValidation(brandForm, {
    fields: {
      brandName: {
        validators: {
          notEmpty: {
            message: 'Please enter brand name'
          }
        }
      },
      commissionRate: {
        validators: {
          notEmpty: {
            message: 'Please enter commission rate'
          },
          between: {
            min: 0,
            max: 100,
            message: 'Commission rate must be between 0 and 100'
          }
        }
      }
    },
    plugins: {
      trigger: new FormValidation.plugins.Trigger(),
      bootstrap5: new FormValidation.plugins.Bootstrap5({
        eleValidClass: '',
        rowSelector: '.mb-6'
      }),
      submitButton: new FormValidation.plugins.SubmitButton(),
      autoFocus: new FormValidation.plugins.AutoFocus()
    }
  });

  // Add new brand
  const addNewBrandBtn = document.querySelector('.brand-data-submit');
  const brandOffcanvasElement = document.getElementById('offcanvasEcommerceBrandList');
  const brandOffcanvas = new bootstrap.Offcanvas(brandOffcanvasElement);

  addNewBrandBtn.addEventListener('click', function (e) {
    const brandId = document.getElementById('brandId').value;
    const isEdit = brandId !== '';

    brandFormValidation.validate().then(function (status) {
      if (status === 'Valid') {
        const brandData = {
          Name: document.getElementById('ecommerce-brand-name').value,
          CommissionRate: parseFloat(document.getElementById('ecommerce-brand-commission').value),
          UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
        };

        if (!isEdit) {
          // Add new brand
          brandData.CreatedAt = firebase.firestore.FieldValue.serverTimestamp();

          brandsCollection.add(brandData)
            .then((docRef) => {
              toastr.success('Brand added successfully', 'Success');
              brandOffcanvas.hide();
              brandForm.reset();
              fetchBrands();
            })
            .catch((error) => {
              console.error("Error adding brand: ", error);
              toastr.error('Failed to add brand: ' + error.message, 'Error');
            });
        } else {
          // Update existing brand
          brandsCollection.doc(brandId).update(brandData)
            .then(() => {
              toastr.success('Brand updated successfully', 'Success');
              brandOffcanvas.hide();
              brandForm.reset();
              document.getElementById('brandId').value = '';
              document.querySelector('.brand-data-submit').textContent = 'Add';
              fetchBrands();
            })
            .catch((error) => {
              console.error("Error updating brand: ", error);
              toastr.error('Failed to update brand: ' + error.message, 'Error');
            });
        }
      }
    });
  });

  // Edit brand
  $(document).on('click', '.edit-brand', function () {
    const brandId = $(this).data('id');
    const brand = brandsData.find(b => b.id === brandId);

    if (brand) {
      document.getElementById('brandId').value = brand.id;
      document.getElementById('ecommerce-brand-name').value = brand.name;
      document.getElementById('ecommerce-brand-commission').value = brand.commissionRate;

      document.querySelector('.brand-data-submit').textContent = 'Update';
      document.getElementById('offcanvasEcommerceBrandListLabel').textContent = 'Edit Brand';

      brandOffcanvas.show();
    } else {
      toastr.error('Brand not found', 'Error');
    }
  });

  // Delete brand with confirmation dialog
  $(document).on('click', '.delete-brand', function () {
    const brandId = $(this).data('id');
    const brand = brandsData.find(b => b.id === brandId);

    if (!brand) {
      toastr.error('Brand not found', 'Error');
      return;
    }

    // Create and show the confirmation modal
    const confirmModal = `
      <div class="modal fade" id="deleteBrandConfirmModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Confirm Delete</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <p>Are you sure you want to delete the brand "${brand.name}"?</p>
              <p class="text-danger">This action cannot be undone and may affect products associated with this brand.</p>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="button" class="btn btn-danger" id="confirmDeleteBrandBtn">Delete</button>
            </div>
          </div>
        </div>
      </div>
    `;

    // Remove any existing modal
    $('#deleteBrandConfirmModal').remove();

    // Add the modal to the DOM
    $('body').append(confirmModal);

    // Initialize the modal
    const modal = new bootstrap.Modal(document.getElementById('deleteBrandConfirmModal'));
    modal.show();

    // Handle confirm button click
    $('#confirmDeleteBrandBtn').on('click', function () {
      // Delete the brand
      brandsCollection.doc(brandId).delete()
        .then(() => {
          modal.hide();
          toastr.success('Brand deleted successfully', 'Success');
          fetchBrands();
        })
        .catch((error) => {
          console.error("Error removing brand: ", error);
          toastr.error('Failed to delete brand: ' + error.message, 'Error');
        });
    });

    // Clean up event listener when modal is hidden
    $('#deleteBrandConfirmModal').on('hidden.bs.modal', function () {
      $('#confirmDeleteBrandBtn').off('click');
      $(this).remove();
    });
  });

  // Reset form when offcanvas is hidden
  brandOffcanvasElement.addEventListener('hidden.bs.offcanvas', function () {
    brandForm.reset();
    document.getElementById('brandId').value = '';
    document.querySelector('.brand-data-submit').textContent = 'Add';
    document.getElementById('offcanvasEcommerceBrandListLabel').textContent = 'Add Brand';
    brandFormValidation.resetForm();
  });

  // Listen for real-time updates to brands
  function setupRealtimeBrandListeners() {
    // Unsubscribe from any existing listeners
    if (window.brandUnsubscribe) {
      window.brandUnsubscribe();
    }

    // Set up a new listener
    window.brandUnsubscribe = brandsCollection.onSnapshot((snapshot) => {
      let hasChanges = false;

      snapshot.docChanges().forEach((change) => {
        hasChanges = true;
      });

      if (hasChanges) {
        fetchBrands(); // Refresh the data
      }
    }, (error) => {
      console.error("Error in real-time listener:", error);
    });
  }

  // Initialize tab event listeners to ensure proper DataTable rendering
  $('#brands-tab').on('shown.bs.tab', function (e) {
    // Adjust the DataTable columns when the tab becomes visible
    brandTable.columns.adjust().draw();
  });

  // Initial load - only if the brands tab is visible or when it becomes visible
  if ($('#brands-tab').hasClass('active')) {
    fetchBrands();
    setupRealtimeBrandListeners();
  } else {
    $('#brands-tab').on('shown.bs.tab', function (e) {
      // Only fetch if we haven't already
      if (brandsData.length === 0) {
        fetchBrands();
        setupRealtimeBrandListeners();
      }
    });
  }
});

