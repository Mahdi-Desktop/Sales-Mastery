// Initialize Firebase
document.addEventListener('DOMContentLoaded', function () {
  // Firebase configuration is passed from the server via the firebaseConfig variable

  // Initialize Firebase
  if (!firebase.apps.length) {
    firebase.initializeApp(firebaseConfig);
  }

  const db = firebase.firestore();
  const categoriesCollection = db.collection('categories');
  const brandsCollection = db.collection('brands');

  let brandsData = []; // To store brands for the dropdown
  let categoriesData = []; // To store categories data

  // Configure toastr (Vuexy toast notifications)
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

  // Initialize DataTable with explicit column definitions to avoid warnings
  const categoryTable = $('.datatables-category-list').DataTable({
    processing: true,
    serverSide: false,
    data: [], // Start with empty data
    columns: [
      { data: null, defaultContent: '' }, // Responsive control column
      { data: null, defaultContent: '' }, // Checkbox column
      { data: 'name', defaultContent: '' }, // Category name
      { data: 'brandName', defaultContent: '' }, // Brand name
      { data: 'description', defaultContent: '' }, // Description
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
                        <span class="avatar-initial rounded-2 bg-label-primary">${full.name ? full.name.charAt(0) : 'C'}</span>
                      </div>
                    </div>
                    <div class="d-flex flex-column">
                      <span class="text-nowrap fw-medium">${full.name || 'Unnamed Category'}</span>
                    </div>
                  </div>`;
        }
      },
      {
        targets: 3,
        render: function (data, type, full) {
          return `<div class="text-sm-end">
                    <span class="text-nowrap fw-medium">${full.brandName || 'No Brand'}</span>
                  </div>`;
        }
      },
      {
        targets: 4,
        render: function (data, type, full) {
          // Strip HTML tags for display in table
          const description = full.description ?
            full.description.replace(/<[^>]*>?/gm, '').substring(0, 50) +
            (full.description.length > 50 ? '...' : '') :
            '';

          return `<div class="text-sm-end">
                    <span class="text-nowrap">${description}</span>
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
                    <a href="javascript:;" class="text-body edit-record" data-id="${full.id}"><i class="ti ti-edit ti-sm me-2"></i></a>
                    <a href="javascript:;" class="text-body delete-record" data-id="${full.id}"><i class="ti ti-trash ti-sm mx-2"></i></a>
                  </div>`;
        }
      }
    ],
    order: [[2, 'asc']],
    dom: '<"card-header d-flex flex-column flex-md-row align-items-md-center py-md-0"<"me-auto"<"d-flex align-items-center"f<"dt-action-buttons ms-3"B>>>><"row"<"col-12"tr>><"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
    lengthMenu: [7, 10, 25, 50, 75, 100],
    buttons: [
      {
        text: '<i class="ti ti-plus me-0 me-sm-1 ti-xs"></i><span class="d-none d-sm-inline-block">Add Category</span>',
        className: 'btn btn-primary',
        attr: {
          'data-bs-toggle': 'offcanvas',
          'data-bs-target': '#offcanvasEcommerceCategoryList'
        }
      }
    ],
    responsive: {
      details: {
        display: $.fn.dataTable.Responsive.display.modal({
          header: function (row) {
            var data = row.data();
            return 'Details of ' + (data.name || 'Category');
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
      searchPlaceholder: 'Search categories...',
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
      // Check if there are no records
      if ($(this).find('tbody tr').length === 0) {
        // If no records found after search
        if ($(this).DataTable().search() !== '') {
          // Show toast notification instead of error
          toastr.info('No matching categories found. Try a different search term.', 'No Results');
        }
      }
    }
  });

  // Add event listener for search to clear toast when user starts a new search
  $('.dataTables_filter input').on('keyup', function () {
    // Clear any existing toasts when user starts typing a new search
    toastr.clear();
  });

  // Fetch brands for dropdown
  function fetchBrands() {
    brandsData = [];
    return brandsCollection.get().then((querySnapshot) => {
      querySnapshot.forEach((doc) => {
        const data = doc.data();
        brandsData.push({
          id: doc.id,
          name: data.Name || 'Unknown Brand',
          path: `brands/${doc.id}`
        });
      });

      // Populate brand dropdown
      const brandSelect = $('#brand-select');
      brandSelect.empty();
      brandSelect.append('<option value="">Select Brand</option>');

      brandsData.forEach(brand => {
        // Store the reference path as the value
        brandSelect.append(`<option value="${brand.path}">${brand.name}</option>`);
      });

      // Initialize select2 for better UX
      $('#brand-select').select2({
        dropdownParent: $('#offcanvasEcommerceCategoryList'),
        placeholder: 'Select a brand'
      });
    }).catch((error) => {
      console.error("Error getting brands: ", error);
      toastr.error('Failed to load brands', 'Error');
    });
  }

  // Extract brand ID from reference path
  function extractBrandId(brandPath) {
    if (!brandPath) return null;

    // Handle string path like "brands/cpDJ5lTlF1vkfcOTMP89"
    if (typeof brandPath === 'string') {
      const parts = brandPath.split('/');
      return parts.length > 1 ? parts[1] : null;
    }

    // Handle reference object with path property
    if (brandPath.path) {
      const parts = brandPath.path.split('/');
      return parts.length > 1 ? parts[1] : null;
    }

    return null;
  }

  // Fetch categories from Firestore
  function fetchCategories() {
    categoriesData = [];
    categoriesCollection.get().then((querySnapshot) => {
      const promises = [];

      querySnapshot.forEach((doc) => {
        const data = doc.data();
        const category = {
          id: doc.id,
          name: data.Name || '',
          description: data.Description || '',
          brandId: data.BrandId || '',
          brandName: 'Loading...',
          createdAt: data.CreatedAt ? data.CreatedAt.toDate() : new Date(),
          updatedAt: data.UpdatedAt ? data.UpdatedAt.toDate() : new Date()
        };

        // If there's a brand reference, fetch the brand name
        if (data.BrandId) {
          // Handle both reference type and string path
          let brandRef;
          if (typeof data.BrandId === 'string') {
            // It's a string path like "brands/cpDJ5lTlF1vkfcOTMP89"
            brandRef = db.doc(data.BrandId);
          } else if (data.BrandId.path) {
            // It's a reference object with a path property
            brandRef = db.doc(data.BrandId.path);
            category.brandId = data.BrandId.path; // Store the path for later use
          } else {
            // Unknown format
            category.brandName = 'Invalid Brand Reference';
            promises.push(Promise.resolve(category));
            return;
          }

          const promise = brandRef.get().then(brandDoc => {
            if (brandDoc.exists) {
              category.brandName = brandDoc.data().Name || 'Unknown Brand';
            } else {
              category.brandName = 'Brand Not Found';
            }
            return category;
          }).catch(error => {
            console.error("Error fetching brand:", error);
            category.brandName = 'Error Loading Brand';
            return category;
          });

          promises.push(promise);
        } else {
          category.brandName = 'No Brand';
          promises.push(Promise.resolve(category));
        }
      });

      Promise.all(promises).then(results => {
        categoriesData = results;
        categoryTable.clear().rows.add(categoriesData).draw();

        // Show message if no categories exist
        if (categoriesData.length === 0) {
          toastr.info('No categories found. Click "Add Category" to create one.', 'No Categories');
        }
      });
    }).catch((error) => {
      console.error("Error getting categories: ", error);
      toastr.error('Failed to load categories', 'Error');
    });
  }

  // Initialize Quill editor
  const categoryDescriptionEditor = new Quill('#ecommerce-category-description', {
    modules: {
      toolbar: '.comment-toolbar'
    },
    placeholder: 'Category Description',
    theme: 'snow'
  });

  // Form validation and submission
  const categoryForm = document.getElementById('eCommerceCategoryListForm');
  const formValidation = FormValidation.formValidation(categoryForm, {
    fields: {
      categoryName: {
        validators: {
          notEmpty: {
            message: 'Please enter category name'
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

  // Add new category
  const addNewCategoryBtn = document.querySelector('.data-submit');
  const offcanvasElement = document.getElementById('offcanvasEcommerceCategoryList');
  const offcanvas = new bootstrap.Offcanvas(offcanvasElement);

  addNewCategoryBtn.addEventListener('click', function (e) {
    const categoryId = document.getElementById('categoryId').value;
    const isEdit = categoryId !== '';

    formValidation.validate().then(function (status) {
      if (status === 'Valid') {
        const brandId = document.getElementById('brand-select').value;

        const categoryData = {
          Name: document.getElementById('ecommerce-category-name').value,
          Description: categoryDescriptionEditor.root.innerHTML,
          UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
        };

        // Only set BrandId if a brand is selected
        if (brandId) {
          // Store as a string path (Firestore will convert to reference on server)
          categoryData.BrandId = brandId;
        } else {
          // If no brand is selected, set to null
          categoryData.BrandId = null;
        }

        if (!isEdit) {
          // Add new category
          categoryData.CreatedAt = firebase.firestore.FieldValue.serverTimestamp();

          categoriesCollection.add(categoryData)
            .then((docRef) => {
              toastr.success('Category added successfully', 'Success');
              offcanvas.hide();
              categoryForm.reset();
              categoryDescriptionEditor.root.innerHTML = '';
              fetchCategories();
            })
            .catch((error) => {
              console.error("Error adding category: ", error);
              toastr.error('Failed to add category: ' + error.message, 'Error');
            });
        } else {
          // Update existing category
          categoriesCollection.doc(categoryId).update(categoryData)
            .then(() => {
              toastr.success('Category updated successfully', 'Success');
              offcanvas.hide();
              categoryForm.reset();
              categoryDescriptionEditor.root.innerHTML = '';
              document.getElementById('categoryId').value = '';
              document.querySelector('.data-submit').textContent = 'Add';
              fetchCategories();
            })
            .catch((error) => {
              console.error("Error updating category: ", error);
              toastr.error('Failed to update category: ' + error.message, 'Error');
            });
        }
      }
    });
  });

  // Edit category
  $(document).on('click', '.edit-record', function () {
    const categoryId = $(this).data('id');
    const category = categoriesData.find(cat => cat.id === categoryId);

    if (category) {
      document.getElementById('categoryId').value = category.id;
      document.getElementById('ecommerce-category-name').value = category.name;

      // Set the brand dropdown value
      if (category.brandId) {
        // Make sure to use the full path format
        $('#brand-select').val(category.brandId).trigger('change');
      } else {
        $('#brand-select').val('').trigger('change');
      }

      // Set description in Quill editor
      categoryDescriptionEditor.root.innerHTML = category.description || '';

      document.querySelector('.data-submit').textContent = 'Update';
      document.getElementById('offcanvasEcommerceCategoryListLabel').textContent = 'Edit Category';

      offcanvas.show();
    } else {
      toastr.error('Category not found', 'Error');
    }
  });

  // Delete category with confirmation dialog
  $(document).on('click', '.delete-record', function () {
    const categoryId = $(this).data('id');
    const category = categoriesData.find(cat => cat.id === categoryId);

    if (!category) {
      toastr.error('Category not found', 'Error');
      return;
    }

    // Create and show the confirmation modal
    const confirmModal = `
      <div class="modal fade" id="deleteConfirmModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Confirm Delete</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <p>Are you sure you want to delete the category "${category.name}"?</p>
              <p class="text-danger">This action cannot be undone.</p>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="button" class="btn btn-danger" id="confirmDeleteBtn">Delete</button>
            </div>
          </div>
        </div>
      </div>
    `;

    // Remove any existing modal
    $('#deleteConfirmModal').remove();

    // Add the modal to the DOM
    $('body').append(confirmModal);

    // Initialize the modal
    const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
    modal.show();

    // Handle confirm button click
    $('#confirmDeleteBtn').on('click', function () {
      // Delete the category
      categoriesCollection.doc(categoryId).delete()
        .then(() => {
          modal.hide();
          toastr.success('Category deleted successfully', 'Success');
          fetchCategories();
        })
        .catch((error) => {
          console.error("Error removing category: ", error);
          toastr.error('Failed to delete category: ' + error.message, 'Error');
        });
    });

    // Clean up event listener when modal is hidden
    $('#deleteConfirmModal').on('hidden.bs.modal', function () {
      $('#confirmDeleteBtn').off('click');
      $(this).remove();
    });
  });

  // Reset form when offcanvas is hidden
  offcanvasElement.addEventListener('hidden.bs.offcanvas', function () {
    categoryForm.reset();
    categoryDescriptionEditor.root.innerHTML = '';
    document.getElementById('categoryId').value = '';
    document.querySelector('.data-submit').textContent = 'Add';
    document.getElementById('offcanvasEcommerceCategoryListLabel').textContent = 'Add Category';
    formValidation.resetForm();
    $('#brand-select').val('').trigger('change');
  });

  // Listen for real-time updates to categories
  function setupRealtimeListeners() {
    // Unsubscribe from any existing listeners
    if (window.categoryUnsubscribe) {
      window.categoryUnsubscribe();
    }

    // Set up a new listener
    window.categoryUnsubscribe = categoriesCollection.onSnapshot((snapshot) => {
      let hasChanges = false;

      snapshot.docChanges().forEach((change) => {
        hasChanges = true;
      });

      if (hasChanges) {
        fetchCategories(); // Refresh the data
      }
    }, (error) => {
      console.error("Error in real-time listener:", error);
    });
  }

  // Initial load
  fetchBrands().then(() => {
    fetchCategories();
    setupRealtimeListeners();
  });
});

