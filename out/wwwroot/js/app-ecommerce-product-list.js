/**
 * Product List
 * This script handles displaying and managing products from Firestore
 */
document.addEventListener('DOMContentLoaded', function () {
  'use strict';

  console.log("DOM loaded, initializing product list page...");

  // Initialize Firebase
  if (!firebase.apps.length) {
    try {
      firebase.initializeApp(firebaseConfig);
      console.log("Firebase initialized successfully");
    } catch (error) {
      console.error("Firebase initialization error:", error);
      toastr.error('Firebase initialization failed. Please check console for details.', 'Error');
      return;
    }
  }

  const db = firebase.firestore();
  const productsCollection = db.collection('products');

  // Configure toastr notifications
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

  // DOM elements for statistics
  const totalProductsEl = document.getElementById('total-products');
  const inStockProductsEl = document.getElementById('in-stock-products');
  const outOfStockProductsEl = document.getElementById('out-of-stock-products');
  const totalValueEl = document.getElementById('total-value');

  // DataTable initialization
  let dt_product_table = $('.datatables-products');
  let dt_products;

  // Load products from Firestore
  async function loadProducts() {
    try {
      console.log("Loading products from Firestore...");
      const snapshot = await productsCollection.get();
      console.log(`Loaded ${snapshot.size} products`);

      const products = [];
      snapshot.forEach(doc => {
        const data = doc.data();
        products.push({
          productId: doc.id,
          name: data.Name || 'Unnamed Product',
          sku: data.SKU || '',
          price: data.Price || 0,
          discount: data.Discount || 0,
          stock: data.Stock || 0,
          commission: data.Commission || 0,
          image: data.Image || [],
          brandId: data.BrandId || '',
          categoryId: data.CategoryId || ''
        });
      });

      // Update statistics
      updateStatistics(products);

      return products;
    } catch (error) {
      console.error("Error loading products:", error);
      toastr.error('Failed to load products. Please try again.', 'Error');
      return [];
    }
  }

  // Update dashboard statistics
  function updateStatistics(products) {
    const totalProducts = products.length;
    const inStockProducts = products.filter(p => p.stock > 0).length;
    const outOfStockProducts = products.filter(p => p.stock <= 0).length;
    const totalValue = products.reduce((sum, p) => sum + (p.price * p.stock), 0);

    totalProductsEl.textContent = totalProducts;
    inStockProductsEl.textContent = inStockProducts;
    outOfStockProductsEl.textContent = outOfStockProducts;
    totalValueEl.textContent = '$' + totalValue.toFixed(2);

    console.log("Statistics updated:", { totalProducts, inStockProducts, outOfStockProducts, totalValue });
  }

  // Delete a product
  async function deleteProduct(productId) {
    try {
      console.log("Deleting product:", productId);
      await productsCollection.doc(productId).delete();
      console.log("Product deleted successfully");
      toastr.success('Product deleted successfully', 'Success');

      // Reload the table
      dt_products.ajax.reload();
    } catch (error) {
      console.error("Error deleting product:", error);
      toastr.error('Failed to delete product. Please try again.', 'Error');
    }
  }

  // Initialize the DataTable
  async function initDataTable() {
    if (dt_product_table.length) {
      // Initialize with empty data first
      dt_products = dt_product_table.DataTable({
        ajax: {
          url: '/Ecommerce/GetProductsJson',
          dataSrc: function (json) {
            console.log('API response:', json);

            // Check if we have data
            if (!json || json.error) {
              console.warn('Error in API response:', json.error || 'Unknown error');
              toastr.error('Failed to load products from API', 'Error');
              return [];
            }

            // Transform data if needed
            return json.map(item => {
              return {
                ...item,
                stock: parseInt(item.stock) || 0 // Ensure stock is a number
              };
            });
          },
          error: function (xhr, error, thrown) {
            console.error('DataTables error:', error, thrown);
            console.error('Response:', xhr.responseText);

            // If API fails, try to load directly from Firestore
            loadProducts().then(products => {
              // Update the table with Firestore data
              dt_products.clear().rows.add(products).draw();
            });
          }
        },
        columns: [
          { data: '' }, // Responsive control column
          { data: 'image' }, // Product image
          { data: 'name' }, // Product name
          { data: 'sku' }, // SKU
          { data: 'price' }, // Price
          { data: 'discount' }, // Discount
          { data: 'stock' }, // Stock
          { data: 'commission' }, // Commission
          { data: '' } // Actions
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
            // Product image
            targets: 1,
            orderable: false,
            searchable: false,
            render: function (data, type, full) {
              console.log("Image data:", data); // Debug log

              // Check if we have valid image data
              if (data && data.length > 0 && data[0]) {
                // Use the first image from Firebase Storage
                return `<img src="${data[0]}" alt="${full.name}" class="rounded-2" height="40" width="40" onerror="this.onerror=null; this.src='/assets/img/elements/1.jpg';">`;
              }

              // Use a default image that definitely exists in the template
              return '<img src="/assets/img/elements/1.jpg" alt="Product" class="rounded-2" height="40" width="40">';
            }
          },
          {
            // Price
            targets: 4,
            render: function (data) {
              // Ensure it's a number and format it properly
              const price = parseFloat(data) || 0;
              return '$' + price.toFixed(2);
            }
          },
          {
            // Discount
            targets: 5,
            render: function (data) {
              // Ensure it's a number or default to 0
              const discount = parseInt(data) || 0;
              return discount + '%';
            }
          },
          {
            // Stock
            targets: 6,
            render: function (data) {
              // Ensure it's an integer
              const stockValue = parseInt(data) || 0;
              const stockClass = stockValue <= 0 ? 'bg-label-danger' : (stockValue <= 10 ? 'bg-label-warning' : 'bg-label-success');
              return '<span class="badge ' + stockClass + '">' + stockValue + '</span>';
            }
          },
          {
            // Commission
            targets: 7,
            render: function (data) {
              // Ensure it's a number and format it properly
              const commission = parseFloat(data) || 0;
              return commission.toFixed(2) + '%';
            }
          },
          {
            // Actions
            targets: -1,
            title: 'Actions',
            orderable: false,
            searchable: false,
            render: function (data, type, full) {
              return (
                '<div class="d-inline-block">' +
                '<a href="javascript:;" class="btn btn-sm btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical"></i></a>' +
                '<div class="dropdown-menu dropdown-menu-end">' +
                '<a href="/Ecommerce/ProductEdit?id=' + full.productId + '" class="dropdown-item">Edit</a>' +
                '<a href="javascript:;" class="dropdown-item delete-record">Delete</a>' +
                '</div>' +
                '</div>'
              );
            }
          }
        ],
        order: [[2, 'asc']], // Order by product name
        dom: '<"card-header d-flex flex-wrap py-3"<"me-5"f><"dt-action-buttons text-end pt-3 pt-md-0"B>><"row"<"col-sm-12"t>><"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
        buttons: [
          {
            text: '<i class="ti ti-plus me-0 me-sm-1"></i><span class="d-none d-sm-inline-block">Add Product</span>',
            className: 'btn btn-primary',
            action: function () {
              window.location.href = '/Ecommerce/ProductAdd';
            }
          }
        ],
        initComplete: function () {
          // Adding product name filter
          this.api()
            .columns(2) // Product name column
            .every(function () {
              var column = this;
              var select = $('<select class="form-select"><option value="">All Products</option></select>')
                .appendTo('.product_category')
                .on('change', function () {
                  var val = $.fn.dataTable.util.escapeRegex($(this).val());
                  column.search(val ? '^' + val + '$' : '', true, false).draw();
                });

              column
                .data()
                .unique()
                .sort()
                .each(function (d) {
                  select.append('<option value="' + d + '">' + d + '</option>');
                });
            });

          // Adding stock status filter
          this.api()
            .columns(6) // Stock column
            .every(function () {
              var column = this;
              var select = $('<select class="form-select"><option value="">All Status</option></select>')
                .appendTo('.product_stock')
                .on('change', function () {
                  var val = $(this).val();
                  if (val === 'In Stock') {
                    column.search('^[1-9]\\d*$', true, false).draw();
                  } else if (val === 'Out of Stock') {
                    column.search('^0$', true, false).draw();
                  } else {
                    column.search('').draw();
                  }
                });

              select.append('<option value="In Stock">In Stock</option>');
              select.append('<option value="Out of Stock">Out of Stock</option>');
            });
        }
      });

      // Handle delete button click
      $('.datatables-products tbody').on('click', '.delete-record', function () {
        var data = dt_products.row($(this).closest('tr')).data();

        if (confirm('Are you sure you want to delete this product?')) {
          deleteProduct(data.productId);
        }
      });

      // If API fails, load directly from Firestore
      loadProducts().then(products => {
        // Update statistics even if we're using the API
        updateStatistics(products);
      });
    }
  }

  // Initialize the page
  initDataTable();
});

