document.addEventListener('DOMContentLoaded', function () {
  // Parse user data from the embedded JSON
  window.userData = null;
  try {
    const userDataElement = document.getElementById('user-data');
    if (userDataElement) {
      window.userData = JSON.parse(userDataElement.textContent);
    }
  } catch (error) {
    console.error('Error parsing user data:', error);
  }

  // Initialize Firebase
  const firebaseConfig = {
    apiKey: "AIzaSyACWsakIQomRmJZShEOrXJ2z-XQOSr9Q5g",
    authDomain: "asp-sales.firebaseapp.com",
    projectId: "asp-sales",
    storageBucket: "asp-sales.firebasestorage.app",
    messagingSenderId: "277356792073",
    appId: "1:277356792073:web:5d676341f60b446cd96bd8"
  };

  // Initialize Firebase
  if (!window.firebase) {
    console.error("Firebase SDK not loaded!");
    return;
  }

  if (!window.firebase.apps.length) {
    firebase.initializeApp(firebaseConfig);
  }

  window.db = firebase.firestore();

  // Now initialize the shop
  initializeShop(window.db);
});

document.addEventListener('DOMContentLoaded', function () {
  // Parse user data from the embedded JSON
  window.userData = null;
  try {
    const userDataElement = document.getElementById('user-data');
    if (userDataElement) {
      window.userData = JSON.parse(userDataElement.textContent);
    }
  } catch (error) {
    console.error('Error parsing user data:', error);
  }

  // Wait for Firebase to be initialized before trying to use it
  if (window.db) {
    // Firebase is already initialized
    initializeShop(window.db);
  } else {
    // Wait for the firebase-ready event
    document.addEventListener('firebase-ready', function () {
      initializeShop(window.db);
    });
  }
});

function initializeShop(db) {
  // Use the userData provided from the view
  const userId = window.userData?.userId || null;
  const isAdmin = window.userData?.isAdmin || false;
  const isAffiliate = window.userData?.isAffiliate || false;
  const isCustomer = window.userData?.isCustomer || false;

  // DOM Elements
  const productsContainer = document.getElementById('productsContainer');
  const filterForm = document.getElementById('filterForm');
  const searchInput = document.getElementById('searchQuery');
  const minPriceInput = document.getElementById('minPrice');
  const maxPriceInput = document.getElementById('maxPrice');
  const categoryFilters = document.querySelectorAll('input[name="category"]');
  const sortBySelect = document.getElementById('sortBy');
  const sortDirectionInputs = document.querySelectorAll('input[name="ascending"]');

  // Cart counter element
  const cartCountElement = document.querySelector('.cart-count');

  // Product data cache
  let allProducts = [];
  let minPrice = 0;
  let maxPrice = 1000;

  // Load products from Firestore
  function loadProducts() {
    if (productsContainer) {
      productsContainer.innerHTML =
        '<div class="col-12 text-center py-5"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>';
    }

    db.collection('products')
      .get()
      .then(snapshot => {
        allProducts = [];

        if (snapshot.empty) {
          if (productsContainer) {
            productsContainer.innerHTML = `
              <div class="col-12">
                <div class="card">
                  <div class="card-body text-center py-5">
                    <i class="ti ti-mood-sad text-primary" style="font-size: 3rem;"></i>
                    <h3 class="mt-3">No Products Found</h3>
                    <p class="mb-3">We couldn't find any products.</p>
                  </div>
                </div>
              </div>
            `;
          }
          return;
        }

        // Process products
        let priceArray = [];

        snapshot.forEach(doc => {
          const product = doc.data();
          product.id = doc.id;
          product.ProductId = product.ProductId || doc.id; // Ensure ProductId exists
          allProducts.push(product);
          priceArray.push(product.Price);
        });

        // Set min and max price values for filter
        if (priceArray.length > 0) {
          minPrice = Math.floor(Math.min(...priceArray));
          maxPrice = Math.ceil(Math.max(...priceArray));

          if (minPriceInput) {
            minPriceInput.min = minPrice;
            minPriceInput.max = maxPrice;
            minPriceInput.value = minPrice;
            const minPriceDisplay = document.getElementById('minPriceDisplay');
            if (minPriceDisplay) minPriceDisplay.textContent = minPrice;
          }

          if (maxPriceInput) {
            maxPriceInput.min = minPrice;
            maxPriceInput.max = maxPrice;
            maxPriceInput.value = maxPrice;
            const maxPriceDisplay = document.getElementById('maxPriceDisplay');
            if (maxPriceDisplay) maxPriceDisplay.textContent = maxPrice;
          }
        }

        // Load categories
        loadCategories();

        // Apply filters and render products
        applyFilters();

        // Handle product details page if we're on that page
        const productDetailsContainer = document.querySelector('.product-details-container');
        if (productDetailsContainer) {
          const urlParams = new URLSearchParams(window.location.search);
          const productId = urlParams.get('id');
          if (productId) {
            loadProductDetails(productId);
          }
        }
      })
      .catch(error => {
        console.error('Error loading products: ', error);
        if (productsContainer) {
          productsContainer.innerHTML = `
            <div class="col-12">
              <div class="card">
                <div class="card-body text-center py-5">
                  <i class="ti ti-alert-triangle text-danger" style="font-size: 3rem;"></i>
                  <h3 class="mt-3">Error Loading Products</h3>
                  <p class="mb-3">An error occurred while loading products. Please try again later.</p>
                </div>
              </div>
            </div>
          `;
        }
      });
  }

  // Load product details for the product details page
  function loadProductDetails(productId) {
    db.collection('products').doc(productId).get()
      .then(doc => {
        if (!doc.exists) {
          console.error('Product not found');
          return;
        }

        const product = doc.data();

        // Load related products
        loadRelatedProducts(product);

        // Add event listener to the add to cart button on the product details page
        const addToCartBtn = document.querySelector('.add-to-cart');
        if (addToCartBtn) {
          addToCartBtn.addEventListener('click', function () {
            const quantity = parseInt(document.querySelector('.quantity-input').value) || 1;
            addProductToCart(productId, quantity);
          });
        }
      })
      .catch(error => {
        console.error('Error loading product details:', error);
      });
  }

  // Load related products for product details page
  function loadRelatedProducts(product) {
    if (!product.CategoryId) return;

    db.collection('products')
      .where('CategoryId', '==', product.CategoryId)
      .where('ProductId', '!=', product.ProductId)
      .limit(4)
      .get()
      .then(snapshot => {
        if (snapshot.empty) return;

        const relatedProductsContainer = document.querySelector('.related-products-container');
        if (!relatedProductsContainer) return;

        let html = '';
        snapshot.forEach(doc => {
          const relatedProduct = doc.data();
          const hasDiscount = relatedProduct.Discount > 0;
          const discountPrice = hasDiscount ? relatedProduct.Price - (relatedProduct.Price * relatedProduct.Discount) / 100 : relatedProduct.Price;
          const discountPercentage = Math.round(relatedProduct.Discount);
          const imageUrl = relatedProduct.Image && relatedProduct.Image.length > 0 ? relatedProduct.Image[0] : '/img/products/default.jpg';

          html += `
            <div class="col-lg-3 col-md-6 col-sm-6">
              <div class="card product-card h-100">
                ${hasDiscount ? `<div class="badge bg-danger discount-badge">-${discountPercentage}%</div>` : ''}
                <div class="card-img-top text-center pt-4">
                  <img src="${imageUrl}" class="product-image" alt="${relatedProduct.Name}" style="height: 150px; object-fit: contain;">
                </div>
                <div class="card-body">
                  <h5 class="card-title">${relatedProduct.Name}</h5>
                  <div class="d-flex justify-content-between align-items-center">
                    <div>
                      ${hasDiscount
              ? `<span class="text-muted text-decoration-line-through me-1">$${relatedProduct.Price.toFixed(2)}</span>
                           <span class="fw-bold text-danger">$${discountPrice.toFixed(2)}</span>`
              : `<span class="fw-bold">$${relatedProduct.Price.toFixed(2)}</span>`}
                    </div>
                  </div>
                </div>
                <div class="card-footer">
                  <a href="/Shop/ProductDetails?id=${relatedProduct.ProductId}" class="btn btn-outline-primary w-100">View Details</a>
                </div>
              </div>
            </div>
          `;
        });

        relatedProductsContainer.innerHTML = html;
      })
      .catch(error => {
        console.error('Error loading related products:', error);
      });
  }

  // Load categories for filter
  function loadCategories() {
    db.collection('categories')
      .get()
      .then(snapshot => {
        const categoryContainer = document.getElementById('categoryContainer');
        if (!categoryContainer) return;

        if (snapshot.empty) {
          return;
        }

        let html = `
          <div class="form-check">
            <input class="form-check-input" type="radio" name="category" id="allCategories"
                   value="" checked>
            <label class="form-check-label" for="allCategories">All Categories</label>
          </div>
        `;

        snapshot.forEach(doc => {
          const category = doc.data();
          html += `
            <div class="form-check">
              <input class="form-check-input" type="radio" name="category"
                     id="category_${doc.id}" value="${doc.id}">
              <label class="form-check-label" for="category_${doc.id}">${category.Name}</label>
            </div>
          `;
        });

        categoryContainer.innerHTML = html;

        // Add event listeners to new category filters
        document.querySelectorAll('input[name="category"]').forEach(input => {
          input.addEventListener('change', applyFilters);
        });
      })
      .catch(error => {
        console.error('Error loading categories: ', error);
      });
  }

  // Apply filters and render products
  function applyFilters() {
    if (!productsContainer) return;

    const searchQuery = searchInput?.value.toLowerCase() || '';
    const minPriceVal = parseFloat(minPriceInput?.value || minPrice);
    const maxPriceVal = parseFloat(maxPriceInput?.value || maxPrice);
    const selectedCategory = document.querySelector('input[name="category"]:checked')?.value || '';
    const sortBy = sortBySelect?.value || 'name';
    const ascending = document.querySelector('input[name="ascending"]:checked')?.value === 'true';

    // Filter products
    let filteredProducts = allProducts.filter(product => {
      // Search filter
      if (
        searchQuery &&
        !product.Name.toLowerCase().includes(searchQuery) &&
        !product.Description.toLowerCase().includes(searchQuery)
      ) {
        return false;
      }

      // Price filter
      if (product.Price < minPriceVal || product.Price > maxPriceVal) {
        return false;
      }

      // Category filter
      if (selectedCategory && product.CategoryId !== `categories/${selectedCategory}`) {
        return false;
      }

      return true;
    });

    // Sort products
    filteredProducts.sort((a, b) => {
      let comparison = 0;

      switch (sortBy) {
        case 'price':
          comparison = a.Price - b.Price;
          break;
        case 'newest':
          const dateA = a.CreatedAt?.toDate() || new Date(0);
          const dateB = b.CreatedAt?.toDate() || new Date(0);
          comparison = dateB - dateA;
          break;
        default: // name
          comparison = a.Name.localeCompare(b.Name);
          break;
      }

      return ascending ? comparison : -comparison;
    });

    renderProducts(filteredProducts);
  }

  // Render products to the page
  function renderProducts(products) {
    if (!productsContainer) return;

    if (products.length === 0) {
      productsContainer.innerHTML = `
        <div class="col-12">
          <div class="card">
            <div class="card-body text-center py-5">
              <i class="ti ti-mood-sad text-primary" style="font-size: 3rem;"></i>
              <h3 class="mt-3">No Products Found</h3>
              <p class="mb-3">We couldn't find any products matching your filters.</p>
              <button id="clearFiltersBtn" class="btn btn-primary">Clear Filters</button>
            </div>
          </div>
        </div>
      `;

      document.getElementById('clearFiltersBtn').addEventListener('click', resetFilters);
      return;
    }

    let html = '';

    products.forEach(product => {
      const hasDiscount = product.Discount > 0;
      const discountPrice = hasDiscount ? product.Price - (product.Price * product.Discount) / 100 : product.Price;
      const discountPercentage = Math.round(product.Discount);
      const imageUrl = product.Image && product.Image.length > 0 ? product.Image[0] : '/img/products/default.jpg';

      html += `
        <div class="col-lg-4 col-md-6 col-sm-6">
          <div class="card product-card h-100">
            ${hasDiscount ? `<div class="badge bg-danger discount-badge">-${discountPercentage}%</div>` : ''}
            <div class="card-img-top text-center pt-4">
              <img src="${imageUrl}" class="product-image" alt="${product.Name}">
            </div>
            <div class="card-body">
              <h5 class="card-title">${product.Name}</h5>
              <p class="card-text text-truncate">${product.Description}</p>
              <div class="d-flex justify-content-between align-items-center">
                <div>
                  ${hasDiscount
          ? `<span class="text-muted text-decoration-line-through me-1">$${product.Price.toFixed(2)}</span>
                       <span class="fw-bold text-danger">$${discountPrice.toFixed(2)}</span>`
          : `<span class="fw-bold">$${product.Price.toFixed(2)}</span>`}
                </div>
                <div>
                  <span class="badge bg-label-success">In Stock: ${product.Stock}</span>
                </div>
              </div>
            </div>
            <div class="card-footer">
              <div class="d-flex justify-content-between">
                <a href="/Shop/ProductDetails?id=${product.ProductId || product.id}" class="btn btn-outline-primary me-2">Details</a>
                                <button class="btn btn-primary add-to-cart-btn" data-product-id="${product.ProductId || product.id}"
                  ${product.Stock <= 0 ? 'disabled' : ''}>
                  <i class="ti ti-shopping-cart me-1"></i> Add to Cart
                </button>
              </div>
            </div>
          </div>
        </div>
      `;
    });

    productsContainer.innerHTML = html;

    // Add event listeners to Add to Cart buttons
    document.querySelectorAll('.add-to-cart-btn').forEach(button => {
      button.addEventListener('click', function (e) {
        const productId = e.currentTarget.getAttribute('data-product-id');
        addProductToCart(productId, 1);
      });
    });
  }

  // Reset filters
  function resetFilters() {
    if (searchInput) searchInput.value = '';
    if (minPriceInput) {
      minPriceInput.value = minPrice;
      document.getElementById('minPriceDisplay').textContent = minPrice;
    }
    if (maxPriceInput) {
      maxPriceInput.value = maxPrice;
      document.getElementById('maxPriceDisplay').textContent = maxPrice;
    }

    const allCategoriesRadio = document.getElementById('allCategories');
    if (allCategoriesRadio) allCategoriesRadio.checked = true;

    if (sortBySelect) sortBySelect.value = 'name';

    const ascendingRadio = document.querySelector('input[name="ascending"][value="true"]');
    if (ascendingRadio) ascendingRadio.checked = true;

    applyFilters();
  }

  // Add to cart function - reusable across pages
  function addProductToCart(productId, quantity) {
    if (!userId) {
      // Redirect to login if not logged in
      window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
      return;
    }

    const cartRef = db.collection('carts').doc(userId);

    // Get the product data
    db.collection('products')
      .doc(productId)
      .get()
      .then(doc => {
        if (!doc.exists) {
          toastr.error('Product not found');
          return;
        }

        const product = doc.data();

        // Check if the product is in stock
        if (product.Stock <= 0) {
          toastr.error('This product is out of stock');
          return;
        }

        // Check if requested quantity is available
        if (product.Stock < quantity) {
          toastr.error(`Only ${product.Stock} items available in stock`);
          return;
        }

        // Update the cart
        return cartRef.get().then(cartDoc => {
          let cart;

          if (!cartDoc.exists) {
            // Create new cart if it doesn't exist
            cart = {
              items: [],
              userId: userId,
              updatedAt: firebase.firestore.FieldValue.serverTimestamp()
            };
          } else {
            cart = cartDoc.data();
            if (!cart.items) cart.items = [];
          }

          // Check if the product is already in the cart
          const existingItemIndex = cart.items.findIndex(item => item.productId === productId);

          if (existingItemIndex !== -1) {
            // Update quantity if already in cart
            cart.items[existingItemIndex].quantity += quantity;
          } else {
            // Add new item to cart
            cart.items.push({
              productId: productId,
              quantity: quantity,
              price: product.Price,
              name: product.Name,
              imageUrl: product.Image && product.Image.length > 0 ? product.Image[0] : null,
              discount: product.Discount || 0,
              brandId: product.BrandId || null,
              subtotal: calculateSubtotal(product.Price, product.Discount || 0, quantity)
            });
          }

          // Recalculate subtotals
          cart.items.forEach(item => {
            item.subtotal = calculateSubtotal(item.price, item.discount, item.quantity);
          });

          cart.updatedAt = firebase.firestore.FieldValue.serverTimestamp();

          // Save cart to Firestore
          return cartRef.set(cart);
        });
      })
      .then(() => {
        toastr.success(`Product added to cart (${quantity} items)`);
        updateCartCount();
      })
      .catch(error => {
        console.error('Error adding to cart: ', error);
        toastr.error('An error occurred while adding to cart');
      });
  }

  // Calculate subtotal with discount
  function calculateSubtotal(price, discount, quantity) {
    const discountedPrice = discount > 0 ? price - (price * discount / 100) : price;
    return discountedPrice * quantity;
  }

  // Update cart count in the header
  function updateCartCount() {
    if (!userId) return;

    db.collection('carts')
      .doc(userId)
      .get()
      .then(doc => {
        let count = 0;

        if (doc.exists) {
          const cart = doc.data();
          if (cart.items && Array.isArray(cart.items)) {
            count = cart.items.reduce((total, item) => total + item.quantity, 0);
          }
        }

        // Update all cart count elements
        document.querySelectorAll('.cart-count').forEach(el => {
          el.textContent = count;
        });
      })
      .catch(error => {
        console.error('Error updating cart count: ', error);
      });
  }

  // Get user ID from storage
  function getStoredUserId() {
    return userId || sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
  }

  // Event listeners for shop page
  if (filterForm) {
    filterForm.addEventListener('submit', function (e) {
      e.preventDefault();
      applyFilters();
    });
  }

  if (searchInput) {
    searchInput.addEventListener('input', function () {
      applyFilters();
    });
  }

  if (minPriceInput) {
    minPriceInput.addEventListener('input', function () {
      document.getElementById('minPriceDisplay').textContent = this.value;
      applyFilters();
    });
  }

  if (maxPriceInput) {
    maxPriceInput.addEventListener('input', function () {
      document.getElementById('maxPriceDisplay').textContent = this.value;
      applyFilters();
    });
  }

  if (categoryFilters) {
    categoryFilters.forEach(input => {
      input.addEventListener('change', applyFilters);
    });
  }

  if (sortBySelect) {
    sortBySelect.addEventListener('change', applyFilters);
  }

  if (sortDirectionInputs) {
    sortDirectionInputs.forEach(input => {
      input.addEventListener('change', applyFilters);
    });
  }

  const resetFiltersBtn = document.getElementById('resetFilters');
  if (resetFiltersBtn) {
    resetFiltersBtn.addEventListener('click', function (e) {
      e.preventDefault();
      resetFilters();
    });
  }

  // Product details page specific functionality
  const quantityInput = document.querySelector('.quantity-input');
  if (quantityInput) {
    quantityInput.addEventListener('change', function () {
      const value = parseInt(this.value);
      if (isNaN(value) || value < 1) {
        this.value = 1;
      }
    });
  }

  // Initialize
  loadProducts();
  updateCartCount();
}

// Make addProductToCart globally available for use in other scripts
window.addProductToCart = function (productId, quantity) {
  const userId = sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
  if (!userId) {
    window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
    return;
  }

  const db = firebase.firestore();
  const cartRef = db.collection('carts').doc(userId);

  // Get the product data
  db.collection('products')
    .doc(productId)
    .get()
    .then(doc => {
      if (!doc.exists) {
        toastr.error('Product not found');
        return;
      }

      const product = doc.data();

      // Check if the product is in stock
      if (product.Stock <= 0) {
        toastr.error('This product is out of stock');
        return;
      }

      // Check if requested quantity is available
      if (product.Stock < quantity) {
        toastr.error(`Only ${product.Stock} items available in stock`);
        return;
      }

      // Update the cart
      return cartRef.get().then(cartDoc => {
        let cart;

        if (!cartDoc.exists) {
          // Create new cart if it doesn't exist
          cart = {
            items: [],
            userId: userId,
            updatedAt: firebase.firestore.FieldValue.serverTimestamp()
          };
        } else {
          cart = cartDoc.data();
          if (!cart.items) cart.items = [];
        }

        // Check if the product is already in the cart
        const existingItemIndex = cart.items.findIndex(item => item.productId === productId);

        if (existingItemIndex !== -1) {
          // Update quantity if already in cart
          cart.items[existingItemIndex].quantity += quantity;
        } else {
          // Add new item to cart
          cart.items.push({
            productId: productId,
            quantity: quantity,
            price: product.Price,
            name: product.Name,
            imageUrl: product.Image && product.Image.length > 0 ? product.Image[0] : null,
            discount: product.Discount || 0,
            brandId: product.BrandId || null,
            subtotal: (product.Discount > 0 ?
              (product.Price - (product.Price * product.Discount / 100)) : product.Price) * quantity
          });
        }

        // Recalculate subtotals
        cart.items.forEach(item => {
          const discountedPrice = item.discount > 0 ?
            item.price - (item.price * item.discount / 100) : item.price;
          item.subtotal = discountedPrice * item.quantity;
        });

        cart.updatedAt = firebase.firestore.FieldValue.serverTimestamp();

        // Save cart to Firestore
        return cartRef.set(cart);
      });
    })
    .then(() => {
      toastr.success(`Product added to cart (${quantity} items)`);

      // Update cart count
      const db = firebase.firestore();
      const userId = sessionStorage.getItem('UserId') || localStorage.getItem('UserId');

      if (userId) {
        db.collection('carts')
          .doc(userId)
          .get()
          .then(doc => {
            let count = 0;

            if (doc.exists) {
              const cart = doc.data();
              if (cart.items && Array.isArray(cart.items)) {
                count = cart.items.reduce((total, item) => total + item.quantity, 0);
              }
            }

            // Update all cart count elements
            document.querySelectorAll('.cart-count').forEach(el => {
              el.textContent = count;
            });
          });
      }
    })
    .catch(error => {
      console.error('Error adding to cart: ', error);
      toastr.error('An error occurred while adding to cart');
    });
};

// Add this to shop.js if you moved the jQuery code there
  document.addEventListener('DOMContentLoaded', function () {
    // Select all elements with class 'add-to-cart'
    document.querySelectorAll('.add-to-cart').forEach(function (button) {
      button.addEventListener('click', function () {
        var productId = this.getAttribute('data-product-id');
        var quantity = this.closest('.card-body').querySelector('.quantity-input').value;
        var productName = this.getAttribute('data-product-name');
        var productPrice = this.getAttribute('data-product-price');
        var productImage = this.getAttribute('data-product-image');

        // Use fetch instead of $.ajax
        fetch('/Shop/AddToCart', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            productId: productId,
            quantity: quantity,
            name: productName,
            price: productPrice,
            imageUrl: productImage
          })
        })
          .then(response => response.json())
          .then(data => {
            if (data.success) {
              toastr.success('Product added to cart!');
              updateCartCount();
            } else {
              toastr.error(data.message);
            }
          })
          .catch(() => {
            toastr.error('Error adding product to cart');
          });
      });
    });

    function updateCartCount() {
      fetch('/Shop/GetCartCount')
        .then(response => response.text())
        .then(count => {
          document.getElementById('cart-count').textContent = count;
        });
    }
  });

