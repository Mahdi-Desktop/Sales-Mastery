/**
 * E-commerce Shop System with Affiliate Marketing
 */
document.addEventListener('DOMContentLoaded', function () {
  'use strict';

  // Initialize Firebase
  if (!firebase.apps.length) {
    firebase.initializeApp(firebaseConfig);
  }

  const db = firebase.firestore();
  const auth = firebase.auth();
  const storage = firebase.storage();

  // Collections
  const productsCollection = db.collection('products');
  const categoriesCollection = db.collection('categories');
  const brandsCollection = db.collection('brands');
  const usersCollection = db.collection('users');
  const ordersCollection = db.collection('orders');
  const cartCollection = db.collection('carts');

  // User information
  let currentUser = null;
  let userRole = null;
  let userId = null;
  let affiliateId = null; // For tracking affiliate referrals

  // Initialize toastr notifications
  toastr.options = {
    closeButton: true,
    newestOnTop: true,
    progressBar: true,
    positionClass: 'toast-top-right',
    preventDuplicates: false,
    showDuration: '300',
    hideDuration: '1000',
    timeOut: '5000',
    extendedTimeOut: '1000',
    showEasing: 'swing',
    hideEasing: 'linear',
    showMethod: 'fadeIn',
    hideMethod: 'fadeOut'
  };

  // Get user information from session
  function initUserInfo() {
    userId = document.getElementById('user-id')?.value || sessionStorage.getItem('userId');
    userRole = document.getElementById('user-role')?.value || sessionStorage.getItem('userRole');

    // Check for affiliate referral in URL
    const urlParams = new URLSearchParams(window.location.search);
    const refParam = urlParams.get('ref');

    if (refParam) {
      // Store affiliate ID in session storage for later use during checkout
      sessionStorage.setItem('affiliateRef', refParam);
      affiliateId = refParam;
    } else {
      affiliateId = sessionStorage.getItem('affiliateRef');
    }

    if (userId) {
      fetchUserDetails();
    }
  }

  // Fetch user details from Firestore
  async function fetchUserDetails() {
    try {
      const userDoc = await usersCollection.doc(userId).get();

      if (userDoc.exists) {
        currentUser = userDoc.data();
        // Update UI elements with user info if needed
        updateUserUI();
      }
    } catch (error) {
      console.error('Error fetching user details:', error);
    }
  }

  // Update UI elements with user information
  function updateUserUI() {
    // Update user name in navbar if element exists
    const userNameElement = document.getElementById('navbar-user-name');
    if (userNameElement && currentUser) {
      userNameElement.textContent = `${currentUser.FirstName || ''} ${currentUser.LastName || ''}`.trim() || 'User';
    }

    // Show/hide elements based on user role
    if (userRole === '1') { // Admin
      document.querySelectorAll('.admin-only').forEach(el => el.classList.remove('d-none'));
    } else if (userRole === '2') { // Affiliate
      document.querySelectorAll('.affiliate-only').forEach(el => el.classList.remove('d-none'));
    }

    // Show stock information only for admin and affiliate
    if (userRole === '1' || userRole === '2') {
      document.querySelectorAll('.stock-info').forEach(el => el.classList.remove('d-none'));
    }
  }

  // ===== PRODUCT LISTING FUNCTIONS =====

  // Fetch and display products
  async function fetchProducts(filters = {}) {
    try {
      let query = productsCollection;

      // Apply filters
      if (filters.category) {
        query = query.where('CategoryId', '==', filters.category);
      }

      if (filters.minPrice && filters.maxPrice) {
        query = query.where('Price', '>=', filters.minPrice)
          .where('Price', '<=', filters.maxPrice);
      }

      if (filters.brand) {
        query = query.where('BrandId', '==', filters.brand);
      }

      // Get products
      const snapshot = await query.get();
      const products = [];

      snapshot.forEach(doc => {
        products.push({
          id: doc.id,
          ...doc.data()
        });
      });

      // Apply search filter client-side if needed
      if (filters.search) {
        const searchLower = filters.search.toLowerCase();
        return products.filter(product =>
          product.Name.toLowerCase().includes(searchLower) ||
          (product.Description && product.Description.toLowerCase().includes(searchLower))
        );
      }

      return products;
    } catch (error) {
      console.error('Error fetching products:', error);
      toastr.error('Failed to load products');
      return [];
    }
  }

  // Render products to the page
  function renderProducts(products) {
    const productsContainer = document.getElementById('products-container');
    if (!productsContainer) return;

    // Clear container
    productsContainer.innerHTML = '';

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

      document.getElementById('clearFiltersBtn').addEventListener('click', clearFilters);
      return;
    }

    // Render each product
    products.forEach(product => {
      const hasDiscount = product.Discount && product.Discount > 0;
      const discountPrice = hasDiscount ?
        product.Price - (product.Price * product.Discount / 100) :
        product.Price;

      const productCard = document.createElement('div');
      productCard.className = 'col-lg-4 col-md-6 col-sm-6';
      productCard.innerHTML = `
        <div class="card product-card h-100" data-product-id="${product.id}">
          ${hasDiscount ? `
            <div class="badge bg-danger discount-badge">
              -${product.Discount}%
            </div>
          ` : ''}
          <div class="card-img-top text-center pt-4">
            <img src="${product.Image && product.Image.length > 0 ? product.Image[0] : '/img/products/default.jpg'}"
                 class="product-image" alt="${product.Name}">
          </div>
          <div class="card-body">
            <h5 class="card-title">${product.Name}</h5>
            <p class="card-text text-truncate">${product.Description || ''}</p>
            <div class="d-flex justify-content-between align-items-center">
              <div>
                ${hasDiscount ? `
                  <span class="text-muted text-decoration-line-through me-1">
                    $${product.Price.toFixed(2)}
                  </span>
                  <span class="fw-bold text-danger">
                    $${discountPrice.toFixed(2)}
                  </span>
                ` : `
                  <span class="fw-bold">$${product.Price.toFixed(2)}</span>
                `}
              </div>
              <div class="stock-info ${(userRole === '1' || userRole === '2') ? '' : 'd-none'}">
                <span class="badge bg-label-success">
                  In Stock: ${product.Stock}
                </span>
              </div>
            </div>
          </div>
          <div class="card-footer">
            <div class="d-flex justify-content-between">
              <a href="/Shop/ProductDetails?id=${product.id}" class="btn btn-outline-primary me-2">
                Details
              </a>
              <button class="btn btn-primary add-to-cart-btn" 
                      data-product-id="${product.id}"
                      ${product.Stock <= 0 ? 'disabled' : ''}>
                <i class="ti ti-shopping-cart me-1"></i> Add to Cart
              </button>
            </div>
          </div>
        </div>
      `;

      productsContainer.appendChild(productCard);
    });

    // Add event listeners to Add to Cart buttons
    document.querySelectorAll('.add-to-cart-btn').forEach(button => {
      button.addEventListener('click', function () {
        const productId = this.getAttribute('data-product-id');
        addToCart(productId, 1);
      });
    });
  }

  // Apply filters from form
  function applyFilters() {
    const searchQuery = document.getElementById('searchQuery')?.value || '';
    const minPrice = parseFloat(document.getElementById('minPrice')?.value || 0);
    const maxPrice = parseFloat(document.getElementById('maxPrice')?.value || 1000000);
    const category = document.querySelector('input[name="category"]:checked')?.value || '';
    const sortBy = document.getElementById('sortBy')?.value || 'name';
    const ascending = document.querySelector('input[name="ascending"]:checked')?.value === 'true';

    // Fetch products with filters
    fetchProducts({
      search: searchQuery,
      minPrice,
      maxPrice,
      category
    }).then(products => {
      // Sort products
      products.sort((a, b) => {
        let comparison = 0;

        switch (sortBy) {
          case 'price':
            comparison = a.Price - b.Price;
            break;
          case 'newest':
            comparison = (b.CreatedAt?.toDate() || 0) - (a.CreatedAt?.toDate() || 0);
            break;
          default: // name
            comparison = a.Name.localeCompare(b.Name);
            break;
        }

        return ascending ? comparison : -comparison;
      });

      renderProducts(products);
    });
  }

  // Clear all filters
  function clearFilters() {
    // Reset form elements
    if (document.getElementById('searchQuery')) {
      document.getElementById('searchQuery').value = '';
    }

    if (document.getElementById('minPrice')) {
      document.getElementById('minPrice').value = document.getElementById('minPrice').getAttribute('min');
      document.getElementById('minPriceDisplay').textContent = document.getElementById('minPrice').getAttribute('min');
    }

    if (document.getElementById('maxPrice')) {
      document.getElementById('maxPrice').value = document.getElementById('maxPrice').getAttribute('max');
      document.getElementById('maxPriceDisplay').textContent = document.getElementById('maxPrice').getAttribute('max');
    }

    if (document.querySelector('input[name="category"][value=""]')) {
      document.querySelector('input[name="category"][value=""]').checked = true;
    }

    if (document.getElementById('sortBy')) {
      document.getElementById('sortBy').value = 'name';
    }

    if (document.querySelector('input[name="ascending"][value="true"]')) {
      document.querySelector('input[name="ascending"][value="true"]').checked = true;
    }

    // Fetch all products
    fetchProducts().then(renderProducts);
  }

  // ===== PRODUCT DETAILS FUNCTIONS =====

  // Fetch and display product details
  async function fetchProductDetails(productId) {
    try {
      const productDoc = await productsCollection.doc(productId).get();

      if (!productDoc.exists) {
        toastr.error('Product not found');
        return null;
      }

      const product = {
        id: productDoc.id,
        ...productDoc.data()
      };

      // Fetch category details if available
      if (product.CategoryId) {
        const categoryDoc = await categoriesCollection.doc(product.CategoryId).get();
        if (categoryDoc.exists) {
          product.CategoryName = categoryDoc.data().Name;
        }
      }

      // Fetch brand details if available
      if (product.BrandId) {
        const brandDoc = await brandsCollection.doc(product.BrandId).get();
        if (brandDoc.exists) {
          product.BrandName = brandDoc.data().Name;
        }
      }

      return product;
    } catch (error) {
      console.error('Error fetching product details:', error);
      toastr.error('Failed to load product details');
      return null;
    }
  }

  // Render product details to the page
  function renderProductDetails(product) {
    const detailsContainer = document.getElementById('product-details-container');
    if (!detailsContainer || !product) return;

    const hasDiscount = product.Discount && product.Discount > 0;
    const discountPrice = hasDiscount ?
      product.Price - (product.Price * product.Discount / 100) :
      product.Price;

    // Update page title
    document.title = product.Name;

    // Render product image
    const imageContainer = document.getElementById('product-image-container');
    if (imageContainer) {
      imageContainer.innerHTML = `
        <div class="card">
          <div class="card-body text-center p-5">
                        <img src="${product.Image && product.Image.length > 0 ? product.Image[0] : '/img/products/default.jpg'}"
                 class="product-image-large" alt="${product.Name}">
          </div>
        </div>
        ${product.Image && product.Image.length > 1 ? `
          <div class="d-flex mt-3 product-thumbnails">
            ${product.Image.map((img, index) => `
              <div class="thumbnail-wrapper me-2 ${index === 0 ? 'active' : ''}">
                <img src="${img}" class="product-thumbnail" alt="${product.Name} thumbnail ${index + 1}">
              </div>
            `).join('')}
          </div>
        ` : ''}
      `;

      // Add event listeners to thumbnails
      document.querySelectorAll('.product-thumbnail').forEach((thumbnail, index) => {
        thumbnail.addEventListener('click', function () {
          document.querySelector('.product-image-large').src = product.Image[index];
          document.querySelectorAll('.thumbnail-wrapper').forEach(wrapper => wrapper.classList.remove('active'));
          this.parentElement.classList.add('active');
        });
      });
    }

    // Render product info
    detailsContainer.innerHTML = `
      <h2 class="mb-3">${product.Name}</h2>
      
      <div class="mb-4">
        ${hasDiscount ? `
          <span class="text-muted text-decoration-line-through me-2 fs-5">
            $${product.Price.toFixed(2)}
          </span>
          <span class="fw-bold text-danger fs-3">
            $${discountPrice.toFixed(2)}
          </span>
          <span class="badge bg-danger ms-2">-${product.Discount}% OFF</span>
        ` : `
          <span class="fw-bold fs-3">$${product.Price.toFixed(2)}</span>
        `}
      </div>
      
      <div class="mb-4 stock-info ${(userRole === '1' || userRole === '2') ? '' : 'd-none'}">
        <span class="badge bg-label-success fs-6">
          In Stock: ${product.Stock}
        </span>
      </div>
      
      <div class="mb-4">
        ${product.CategoryName ? `<p class="mb-1"><strong>Category:</strong> ${product.CategoryName}</p>` : ''}
        ${product.BrandName ? `<p class="mb-1"><strong>Brand:</strong> ${product.BrandName}</p>` : ''}
        ${product.SKU ? `<p class="mb-1"><strong>SKU:</strong> ${product.SKU}</p>` : ''}
      </div>
      
      <div class="mb-4">
        <h5>Description</h5>
        <div class="product-description">
          ${product.Description || 'No description available.'}
        </div>
      </div>
      
      <div class="mb-4">
        <div class="d-flex align-items-center">
          <label for="quantity" class="me-3">Quantity:</label>
          <div class="input-group" style="width: 150px;">
            <button type="button" class="btn btn-outline-primary" id="decreaseQty">
              <i class="ti ti-minus"></i>
            </button>
            <input type="number" class="form-control text-center" id="quantity" value="1" min="1" max="${product.Stock}">
            <button type="button" class="btn btn-outline-primary" id="increaseQty">
              <i class="ti ti-plus"></i>
            </button>
          </div>
        </div>
      </div>
      
      <div class="d-flex mt-4">
        <button type="button" class="btn btn-primary me-3" id="addToCartBtn" ${product.Stock <= 0 ? 'disabled' : ''}>
          <i class="ti ti-shopping-cart me-1"></i> Add to Cart
        </button>
        <a href="/Shop/Cart" class="btn btn-outline-primary">
          <i class="ti ti-shopping-cart-check me-1"></i> View Cart
        </a>
      </div>
    `;

    // Add event listeners for quantity controls
    document.getElementById('decreaseQty').addEventListener('click', function () {
      const quantityInput = document.getElementById('quantity');
      const currentValue = parseInt(quantityInput.value);
      if (currentValue > 1) {
        quantityInput.value = currentValue - 1;
      }
    });

    document.getElementById('increaseQty').addEventListener('click', function () {
      const quantityInput = document.getElementById('quantity');
      const currentValue = parseInt(quantityInput.value);
      const maxValue = parseInt(quantityInput.getAttribute('max'));
      if (currentValue < maxValue) {
        quantityInput.value = currentValue + 1;
      }
    });

    // Add event listener for Add to Cart button
    document.getElementById('addToCartBtn').addEventListener('click', function () {
      const quantity = parseInt(document.getElementById('quantity').value);
      addToCart(product.id, quantity);
    });
  }

  // ===== CART FUNCTIONS =====

  // Add product to cart
  async function addToCart(productId, quantity) {
    try {
      // Check if user is logged in
      if (!userId) {
        // For non-logged in users, use local storage cart
        addToLocalCart(productId, quantity);
        return;
      }

      // Get product details
      const productDoc = await productsCollection.doc(productId).get();

      if (!productDoc.exists) {
        toastr.error('Product not found');
        return;
      }

      const product = productDoc.data();

      // Check stock
      if (product.Stock < quantity) {
        toastr.error('Not enough stock available');
        return;
      }

      // Get user's cart
      const cartRef = cartCollection.doc(userId);
      const cartDoc = await cartRef.get();

      // Calculate price with discount
      const price = product.Price;
      const discount = product.Discount || 0;
      const discountedPrice = discount > 0 ? price - (price * discount / 100) : price;

      if (!cartDoc.exists) {
        // Create new cart
        await cartRef.set({
          userId: userId,
          items: [{
            productId: productId,
            name: product.Name,
            price: price,
            discountedPrice: discountedPrice,
            discount: discount,
            quantity: quantity,
            imageUrl: product.Image && product.Image.length > 0 ? product.Image[0] : null,
            subTotal: discountedPrice * quantity
          }],
          updatedAt: firebase.firestore.FieldValue.serverTimestamp()
        });
      } else {
        // Update existing cart
        const cart = cartDoc.data();
        const items = cart.items || [];

        // Check if product already in cart
        const existingItemIndex = items.findIndex(item => item.productId === productId);

        if (existingItemIndex >= 0) {
          // Update quantity
          items[existingItemIndex].quantity += quantity;
          items[existingItemIndex].subTotal = items[existingItemIndex].discountedPrice * items[existingItemIndex].quantity;
        } else {
          // Add new item
          items.push({
            productId: productId,
            name: product.Name,
            price: price,
            discountedPrice: discountedPrice,
            discount: discount,
            quantity: quantity,
            imageUrl: product.Image && product.Image.length > 0 ? product.Image[0] : null,
            subTotal: discountedPrice * quantity
          });
        }

        await cartRef.update({
          items: items,
          updatedAt: firebase.firestore.FieldValue.serverTimestamp()
        });
      }

      // Show success message
      toastr.success('Product added to cart');

      // Update cart count in UI
      updateCartCount();

    } catch (error) {
      console.error('Error adding to cart:', error);
      toastr.error('Failed to add product to cart');
    }
  }

  // Add to local storage cart for non-logged in users
  async function addToLocalCart(productId, quantity) {
    try {
      // Get product details
      const productDoc = await productsCollection.doc(productId).get();

      if (!productDoc.exists) {
        toastr.error('Product not found');
        return;
      }

      const product = productDoc.data();

      // Check stock
      if (product.Stock < quantity) {
        toastr.error('Not enough stock available');
        return;
      }

      // Get existing cart from local storage
      let cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };

      // Calculate price with discount
      const price = product.Price;
      const discount = product.Discount || 0;
      const discountedPrice = discount > 0 ? price - (price * discount / 100) : price;

      // Check if product already in cart
      const existingItemIndex = cart.items.findIndex(item => item.productId === productId);

      if (existingItemIndex >= 0) {
        // Update quantity
        cart.items[existingItemIndex].quantity += quantity;
        cart.items[existingItemIndex].subTotal = cart.items[existingItemIndex].discountedPrice * cart.items[existingItemIndex].quantity;
      } else {
        // Add new item
        cart.items.push({
          productId: productId,
          name: product.Name,
          price: price,
          discountedPrice: discountedPrice,
          discount: discount,
          quantity: quantity,
          imageUrl: product.Image && product.Image.length > 0 ? product.Image[0] : null,
          subTotal: discountedPrice * quantity
        });
      }

      // Save cart to local storage
      localStorage.setItem('cart', JSON.stringify(cart));

      // Show success message
      toastr.success('Product added to cart');

      // Update cart count in UI
      updateCartCount();

    } catch (error) {
      console.error('Error adding to local cart:', error);
      toastr.error('Failed to add product to cart');
    }
  }

  // Update cart item quantity
  async function updateCartQuantity(productId, quantity) {
    try {
      // Check if user is logged in
      if (!userId) {
        // For non-logged in users, use local storage cart
        updateLocalCartQuantity(productId, quantity);
        return;
      }

      // Get user's cart
      const cartRef = cartCollection.doc(userId);
      const cartDoc = await cartRef.get();

      if (!cartDoc.exists) {
        toastr.error('Cart not found');
        return;
      }

      const cart = cartDoc.data();
      const items = cart.items || [];

      // Find the item
      const itemIndex = items.findIndex(item => item.productId === productId);

      if (itemIndex < 0) {
        toastr.error('Product not found in cart');
        return;
      }

      // Check product stock
      const productDoc = await productsCollection.doc(productId).get();
      if (productDoc.exists && productDoc.data().Stock < quantity) {
        toastr.error('Not enough stock available');
        return;
      }

      // Update quantity
      items[itemIndex].quantity = quantity;
      items[itemIndex].subTotal = items[itemIndex].discountedPrice * quantity;

      await cartRef.update({
        items: items,
        updatedAt: firebase.firestore.FieldValue.serverTimestamp()
      });

      // Update cart UI
      updateCartUI();

    } catch (error) {
      console.error('Error updating cart:', error);
      toastr.error('Failed to update cart');
    }
  }

  // Update local cart quantity
  function updateLocalCartQuantity(productId, quantity) {
    // Get existing cart from local storage
    let cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };

    // Find the item
    const itemIndex = cart.items.findIndex(item => item.productId === productId);

    if (itemIndex < 0) {
      toastr.error('Product not found in cart');
      return;
    }

    // Update quantity
    cart.items[itemIndex].quantity = quantity;
    cart.items[itemIndex].subTotal = cart.items[itemIndex].discountedPrice * quantity;

    // Save cart to local storage
    localStorage.setItem('cart', JSON.stringify(cart));

    // Update cart UI
    updateCartUI();
  }

  // Remove item from cart
  async function removeFromCart(productId) {
    try {
      // Check if user is logged in
      if (!userId) {
        // For non-logged in users, use local storage cart
        removeFromLocalCart(productId);
        return;
      }

      // Get user's cart
      const cartRef = cartCollection.doc(userId);
      const cartDoc = await cartRef.get();

      if (!cartDoc.exists) {
        toastr.error('Cart not found');
        return;
      }

      const cart = cartDoc.data();
      const items = cart.items || [];

      // Remove the item
      const updatedItems = items.filter(item => item.productId !== productId);

      await cartRef.update({
        items: updatedItems,
        updatedAt: firebase.firestore.FieldValue.serverTimestamp()
      });

      toastr.success('Product removed from cart');

      // Update cart UI
      updateCartUI();

    } catch (error) {
      console.error('Error removing from cart:', error);
      toastr.error('Failed to remove product from cart');
    }
  }

  // Remove from local cart
  function removeFromLocalCart(productId) {
    // Get existing cart from local storage
    let cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };

    // Remove the item
    cart.items = cart.items.filter(item => item.productId !== productId);

    // Save cart to local storage
    localStorage.setItem('cart', JSON.stringify(cart));

    toastr.success('Product removed from cart');

    // Update cart UI
    updateCartUI();
  }

  // Clear cart
  async function clearCart() {
    try {
      // Check if user is logged in
      if (!userId) {
        // For non-logged in users, use local storage cart
        localStorage.removeItem('cart');
        toastr.success('Cart cleared');
        updateCartUI();
        return;
      }

      // Get user's cart
      const cartRef = cartCollection.doc(userId);

      await cartRef.update({
        items: [],
        updatedAt: firebase.firestore.FieldValue.serverTimestamp()
      });

      toastr.success('Cart cleared');

      // Update cart UI
      updateCartUI();

    } catch (error) {
      console.error('Error clearing cart:', error);
      toastr.error('Failed to clear cart');
    }
  }

  // Update cart count in UI
  async function updateCartCount() {
    try {
      let count = 0;

      // Check if user is logged in
      if (userId) {
        // Get user's cart
        const cartDoc = await cartCollection.doc(userId).get();

        if (cartDoc.exists) {
          const cart = cartDoc.data();
          count = cart.items ? cart.items.length : 0;
        }
      } else {
        // For non-logged in users, use local storage cart
        const cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };
        count = cart.items.length;
      }

      // Update UI
      const cartCountElements = document.querySelectorAll('.cart-count');
      cartCountElements.forEach(element => {
        element.textContent = count;
      });

    } catch (error) {
      console.error('Error updating cart count:', error);
    }
  }

  // Fetch and render cart
  async function fetchAndRenderCart() {
    try {
      let cartItems = [];
      let cartTotal = 0;

      // Check if user is logged in
      if (userId) {
        // Get user's cart
        const cartDoc = await cartCollection.doc(userId).get();

        if (cartDoc.exists) {
          const cart = cartDoc.data();
          cartItems = cart.items || [];
        }
      } else {
        // For non-logged in users, use local storage cart
        const cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };
        cartItems = cart.items;
      }

      // Calculate cart total
      cartTotal = cartItems.reduce((total, item) => total + item.subTotal, 0);

      // Render cart
      renderCart(cartItems, cartTotal);

    } catch (error) {
      console.error('Error fetching cart:', error);
      toastr.error('Failed to load cart');
    }
  }

  // Render cart to the page
  function renderCart(cartItems, cartTotal) {
    const cartContainer = document.getElementById('cart-items-container');
    const cartSummaryContainer = document.getElementById('cart-summary-container');

    if (!cartContainer) return;

    if (cartItems.length === 0) {
      // Empty cart
      cartContainer.innerHTML = `
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="ti ti-shopping-cart text-primary" style="font-size: 4rem;"></i>
            <h3 class="mt-4">Your cart is empty</h3>
            <p class="mb-4">Looks like you haven't added anything to your cart yet.</p>
            <a href="/Shop" class="btn btn-primary">Continue Shopping</a>
          </div>
        </div>
      `;

      if (cartSummaryContainer) {
        cartSummaryContainer.innerHTML = '';
      }

      return;
    }

    // Render cart items
    let cartItemsHtml = `
      <div class="card">
        <div class="card-header">
          <h5 class="mb-0">Cart Items</h5>
        </div>
        <div class="table-responsive">
          <table class="table table-bordered">
            <thead>
              <tr>
                <th>Product</th>
                <th>Price</th>
                <th>Quantity</th>
                <th>Subtotal</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
    `;

    cartItems.forEach(item => {
      cartItemsHtml += `
        <tr data-product-id="${item.productId}">
          <td>
            <div class="d-flex align-items-center">
              <div class="avatar avatar-lg me-3">
                <img src="${item.imageUrl || '/img/products/default.jpg'}" alt="${item.name}" class="rounded">
              </div>
              <div>
                <h6 class="mb-0">${item.name}</h6>
                ${item.discount > 0 ? `<span class="badge bg-label-danger">-${item.discount}%</span>` : ''}
              </div>
            </div>
          </td>
          <td>
            ${item.discount > 0 ?
          `<span class="text-muted text-decoration-line-through me-1">$${item.price.toFixed(2)}</span>
               <span class="fw-bold">$${item.discountedPrice.toFixed(2)}</span>` :
          `<span class="fw-bold">$${item.price.toFixed(2)}</span>`
        }
          </td>
          <td>
            <div class="input-group" style="width: 140px;">
              <button type="button" class="btn btn-sm btn-outline-primary decrease-qty" data-product-id="${item.productId}">
                <i class="ti ti-minus"></i>
              </button>
              <input type="number" class="form-control form-control-sm text-center item-quantity" 
                     value="${item.quantity}" min="1" data-product-id="${item.productId}">
              <button type="button" class="btn btn-sm btn-outline-primary increase-qty" data-product-id="${item.productId}">
                <i class="ti ti-plus"></i>
              </button>
            </div>
          </td>
          <td class="item-subtotal">$${item.subTotal.toFixed(2)}</td>
          <td>
            <button type="button" class="btn btn-sm btn-danger remove-item" data-product-id="${item.productId}">
              <i class="ti ti-trash"></i>
            </button>
          </td>
        </tr>
      `;
    });

    cartItemsHtml += `
            </tbody>
          </table>
        </div>
        <div class="card-footer text-end">
          <button type="button" class="btn btn-outline-danger" id="clearCartBtn">
            <i class="ti ti-trash me-1"></i> Clear Cart
          </button>
        </div>
      </div>
    `;

    cartContainer.innerHTML = cartItemsHtml;

    // Render cart summary
    if (cartSummaryContainer) {
      const shippingFee = 10; // Fixed shipping fee
      const totalWithShipping = cartTotal + shippingFee;

      cartSummaryContainer.innerHTML = `
        <div class="card">
          <div class="card-header">
            <h5 class="mb-0">Order Summary</h5>
          </div>
          <div class="card-body">
            <div class="d-flex justify-content-between mb-2">
              <span>Subtotal</span>
              <span id="subtotal">$${cartTotal.toFixed(2)}</span>
            </div>
            <div class="d-flex justify-content-between mb-2">
              <span>Shipping Fee</span>
              <span>$${shippingFee.toFixed(2)}</span>
            </div>
            <hr>
            <div class="d-flex justify-content-between fw-bold">
              <span>Total</span>
                            <span id="total">$${totalWithShipping.toFixed(2)}</span>
            </div>
            <div class="mt-4">
              <a href="/Shop/Checkout" class="btn btn-primary w-100">
                <i class="ti ti-check me-1"></i> Proceed to Checkout
              </a>
            </div>
            <div class="mt-2">
              <a href="/Shop" class="btn btn-outline-secondary w-100">
                <i class="ti ti-arrow-left me-1"></i> Continue Shopping
              </a>
            </div>
          </div>
        </div>
      `;
    }

    // Add event listeners
    document.querySelectorAll('.decrease-qty').forEach(button => {
      button.addEventListener('click', function () {
        const productId = this.getAttribute('data-product-id');
        const input = document.querySelector(`.item-quantity[data-product-id="${productId}"]`);
        const currentValue = parseInt(input.value);

        if (currentValue > 1) {
          input.value = currentValue - 1;
          updateCartQuantity(productId, currentValue - 1);
        }
      });
    });

    document.querySelectorAll('.increase-qty').forEach(button => {
      button.addEventListener('click', function () {
        const productId = this.getAttribute('data-product-id');
        const input = document.querySelector(`.item-quantity[data-product-id="${productId}"]`);
        const currentValue = parseInt(input.value);

        input.value = currentValue + 1;
        updateCartQuantity(productId, currentValue + 1);
      });
    });

    document.querySelectorAll('.item-quantity').forEach(input => {
      input.addEventListener('change', function () {
        const productId = this.getAttribute('data-product-id');
        const quantity = parseInt(this.value);

        if (quantity < 1) {
          this.value = 1;
          updateCartQuantity(productId, 1);
        } else {
          updateCartQuantity(productId, quantity);
        }
      });
    });

    document.querySelectorAll('.remove-item').forEach(button => {
      button.addEventListener('click', function () {
        const productId = this.getAttribute('data-product-id');
        removeFromCart(productId);
      });
    });

    document.getElementById('clearCartBtn')?.addEventListener('click', function () {
      if (confirm('Are you sure you want to clear your cart?')) {
        clearCart();
      }
    });
  }

  // Update cart UI after changes
  function updateCartUI() {
    fetchAndRenderCart();
    updateCartCount();
  }

  // ===== CHECKOUT FUNCTIONS =====

  // Initialize checkout page
  async function initCheckout() {
    try {
      let cartItems = [];
      let cartTotal = 0;

      // Check if user is logged in
      if (userId) {
        // Get user's cart
        const cartDoc = await cartCollection.doc(userId).get();

        if (cartDoc.exists) {
          const cart = cartDoc.data();
          cartItems = cart.items || [];
        }
      } else {
        // For non-logged in users, use local storage cart
        const cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };
        cartItems = cart.items;
      }

      // Calculate cart total
      cartTotal = cartItems.reduce((total, item) => total + item.subTotal, 0);

      // Check if cart is empty
      if (cartItems.length === 0) {
        window.location.href = '/Shop/Cart';
        return;
      }

      // Render checkout summary
      renderCheckoutSummary(cartItems, cartTotal);

      // Pre-fill user information if available
      if (userId && currentUser) {
        prefillCheckoutForm();
      }

      // Add event listener to checkout form
      document.getElementById('checkoutForm')?.addEventListener('submit', function (e) {
        e.preventDefault();
        processCheckout();
      });

    } catch (error) {
      console.error('Error initializing checkout:', error);
      toastr.error('Failed to load checkout page');
    }
  }

  // Render checkout summary
  function renderCheckoutSummary(cartItems, cartTotal) {
    const summaryContainer = document.getElementById('order-summary-container');
    if (!summaryContainer) return;

    const shippingFee = 10; // Fixed shipping fee
    const totalWithShipping = cartTotal + shippingFee;

    let summaryHtml = `
      <div class="table-responsive mb-3">
        <table class="table">
          <tbody>
    `;

    cartItems.forEach(item => {
      summaryHtml += `
        <tr>
          <td class="text-nowrap">
            <div class="d-flex align-items-center">
              <div class="avatar avatar-sm me-2">
                <img src="${item.imageUrl || '/img/products/default.jpg'}" alt="${item.name}" class="rounded">
              </div>
              <div>${item.name} <span class="text-muted">× ${item.quantity}</span></div>
            </div>
          </td>
          <td class="text-end">$${item.subTotal.toFixed(2)}</td>
        </tr>
      `;
    });

    summaryHtml += `
          </tbody>
        </table>
      </div>

      <div class="d-flex justify-content-between mb-2">
        <span>Subtotal</span>
        <span>$${cartTotal.toFixed(2)}</span>
      </div>
      <div class="d-flex justify-content-between mb-2">
        <span>Delivery Fee</span>
        <span>$${shippingFee.toFixed(2)}</span>
      </div>
      <hr>
      <div class="d-flex justify-content-between fw-bold">
        <span>Total</span>
        <span>$${totalWithShipping.toFixed(2)}</span>
      </div>
    `;

    summaryContainer.innerHTML = summaryHtml;
  }

  // Pre-fill checkout form with user information
  function prefillCheckoutForm() {
    if (!currentUser) return;

    // Pre-fill address information if available
    if (currentUser.Address) {
      document.getElementById('address').value = currentUser.Address.Address || '';
      document.getElementById('city').value = currentUser.Address.City || '';
      document.getElementById('state').value = currentUser.Address.State || '';
      document.getElementById('zipCode').value = currentUser.Address.ZipCode || '';
      document.getElementById('phone').value = currentUser.PhoneNumber || '';
    }
  }

  // Process checkout
  async function processCheckout() {
    try {
      // Show loading state
      const submitButton = document.querySelector('#checkoutForm button[type="submit"]');
      submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
      submitButton.disabled = true;

      // Get form data
      const address = document.getElementById('address').value;
      const city = document.getElementById('city').value;
      const state = document.getElementById('state').value;
      const zipCode = document.getElementById('zipCode').value;
      const phone = document.getElementById('phone').value;
      const paymentMethod = document.querySelector('input[name="paymentMethod"]:checked').value;

      // Validate form data
      if (!address || !city || !state || !zipCode || !phone) {
        toastr.error('Please fill in all required fields');
        submitButton.innerHTML = '<i class="ti ti-check me-1"></i> Place Order';
        submitButton.disabled = false;
        return;
      }

      // Get cart items
      let cartItems = [];
      let cartTotal = 0;

      // Check if user is logged in
      if (userId) {
        // Get user's cart
        const cartDoc = await cartCollection.doc(userId).get();

        if (cartDoc.exists) {
          const cart = cartDoc.data();
          cartItems = cart.items || [];
        }
      } else {
        // For non-logged in users, use local storage cart
        const cart = JSON.parse(localStorage.getItem('cart')) || { items: [] };
        cartItems = cart.items;
      }

      // Calculate cart total
      cartTotal = cartItems.reduce((total, item) => total + item.subTotal, 0);

      // Check if cart is empty
      if (cartItems.length === 0) {
        toastr.error('Your cart is empty');
        submitButton.innerHTML = '<i class="ti ti-check me-1"></i> Place Order';
        submitButton.disabled = false;
        return;
      }

      // Create order
      const shippingFee = 10; // Fixed shipping fee
      const totalWithShipping = cartTotal + shippingFee;

      // Get affiliate ID from session storage if available
      const affiliateId = sessionStorage.getItem('affiliateRef');

      // Calculate affiliate commission if applicable
      let affiliateCommission = 0;

      if (affiliateId) {
        // Get commission rates for each product based on brand
        for (const item of cartItems) {
          try {
            const productDoc = await productsCollection.doc(item.productId).get();
            if (productDoc.exists) {
              const product = productDoc.data();
              const commissionRate = product.Commission || 0; // Default to 0 if not set

              // Calculate commission for this item
              const itemCommission = (item.subTotal * commissionRate) / 100;
              affiliateCommission += itemCommission;
            }
          } catch (error) {
            console.error('Error calculating commission for product:', error);
          }
        }
      }

      // Create order object
      const order = {
        CustomerId: userId || null,
        AffiliateId: affiliateId || null,
        Items: cartItems.map(item => ({
          ProductId: item.productId,
          Name: item.name,
          Price: item.price,
          Discount: item.discount,
          Quantity: item.quantity,
          SubTotal: item.subTotal
        })),
        ShippingAddress: {
          Address: address,
          City: city,
          State: state,
          ZipCode: zipCode,
          Phone: phone
        },
        PaymentMethod: paymentMethod,
        Status: 'Pending',
        SubTotal: cartTotal,
        ShippingFee: shippingFee,
        TotalAmount: totalWithShipping,
        AffiliateCommission: affiliateCommission,
        OrderDate: firebase.firestore.FieldValue.serverTimestamp(),
        UpdatedAt: firebase.firestore.FieldValue.serverTimestamp(),
        Notes: ''
      };

      // Add order to Firestore
      const orderRef = await ordersCollection.add(order);
      const orderId = orderRef.id;

      // Update product stock
      for (const item of cartItems) {
        try {
          const productRef = productsCollection.doc(item.productId);
          const productDoc = await productRef.get();

          if (productDoc.exists) {
            const product = productDoc.data();
            const newStock = Math.max(0, product.Stock - item.quantity);

            await productRef.update({
              Stock: newStock,
              UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
            });
          }
        } catch (error) {
          console.error('Error updating product stock:', error);
        }
      }

      // Update user's order history if logged in
      if (userId) {
        try {
          const userRef = usersCollection.doc(userId);
          await userRef.update({
            OrderId: firebase.firestore.FieldValue.arrayUnion(orderRef),
            UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
          });
        } catch (error) {
          console.error('Error updating user order history:', error);
        }
      }

      // Update affiliate's customer and order lists if applicable
      if (affiliateId) {
        try {
          const affiliateRef = usersCollection.doc(affiliateId);
          const affiliateDoc = await affiliateRef.get();

          if (affiliateDoc.exists) {
            // Add customer to affiliate's customer list if not already there
            if (userId) {
              await affiliateRef.update({
                CustomerId: firebase.firestore.FieldValue.arrayUnion(userId),
                OrderId: firebase.firestore.FieldValue.arrayUnion(orderRef),
                UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
              });
            }
          }
        } catch (error) {
          console.error('Error updating affiliate data:', error);
        }
      }

      // Clear cart
      if (userId) {
        await cartCollection.doc(userId).update({
          items: [],
          updatedAt: firebase.firestore.FieldValue.serverTimestamp()
        });
      } else {
        localStorage.removeItem('cart');
      }

      // Clear affiliate reference
      sessionStorage.removeItem('affiliateRef');

      // Redirect to order confirmation page
      window.location.href = `/Shop/OrderConfirmation?id=${orderId}`;

    } catch (error) {
      console.error('Error processing checkout:', error);
      toastr.error('Failed to process your order');

      // Reset button state
      const submitButton = document.querySelector('#checkoutForm button[type="submit"]');
      submitButton.innerHTML = '<i class="ti ti-check me-1"></i> Place Order';
      submitButton.disabled = false;
    }
  }

  // ===== ORDER HISTORY FUNCTIONS =====

  // Fetch and display order history
  async function fetchOrderHistory() {
    try {
      if (!userId) {
        // Redirect to login if not logged in
        window.location.href = '/Auth/LoginBasic';
        return;
      }

      // Get user's orders
      const userDoc = await usersCollection.doc(userId).get();

      if (!userDoc.exists) {
        toastr.error('User not found');
        return;
      }

      const userData = userDoc.data();
      const orderRefs = userData.OrderId || [];
      const orders = [];

      // Fetch each order
      for (const orderRef of orderRefs) {
        try {
          const orderDoc = await db.doc(orderRef.path).get();

          if (orderDoc.exists) {
            orders.push({
              id: orderDoc.id,
              ...orderDoc.data(),
              OrderDate: orderDoc.data().OrderDate?.toDate() || new Date()
            });
          }
        } catch (error) {
          console.error('Error fetching order:', error);
        }
      }

      // Sort orders by date (newest first)
      orders.sort((a, b) => b.OrderDate - a.OrderDate);

      // Render orders
      renderOrderHistory(orders);

    } catch (error) {
      console.error('Error fetching order history:', error);
      toastr.error('Failed to load order history');
    }
  }

  // Render order history
  function renderOrderHistory(orders) {
    const orderHistoryContainer = document.getElementById('order-history-container');
    if (!orderHistoryContainer) return;

    if (orders.length === 0) {
      orderHistoryContainer.innerHTML = `
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="ti ti-receipt text-primary" style="font-size: 4rem;"></i>
            <h3 class="mt-4">No orders found</h3>
            <p class="mb-4">You haven't placed any orders yet.</p>
            <a href="/Shop" class="btn btn-primary">Start Shopping</a>
          </div>
        </div>
      `;
      return;
    }

    let ordersHtml = '';

    orders.forEach(order => {
      const orderDate = new Date(order.OrderDate).toLocaleDateString();
      const statusClass = getStatusClass(order.Status);

      ordersHtml += `
        <div class="card mb-4">
          <div class="card-header d-flex justify-content-between align-items-center">
            <div>
              <h5 class="mb-0">Order #${order.id}</h5>
              <small class="text-muted">Placed on ${orderDate}</small>
            </div>
            <div>
              <span class="badge ${statusClass}">${order.Status}</span>
            </div>
          </div>
          <div class="card-body">
            <div class="table-responsive mb-3">
              <table class="table">
                <thead>
                  <tr>
                    <th>Product</th>
                    <th>Price</th>
                    <th>Quantity</th>
                    <th>Subtotal</th>
                  </tr>
                </thead>
                <tbody>
      `;

      order.Items.forEach(item => {
        ordersHtml += `
          <tr>
            <td>${item.Name}</td>
            <td>$${item.Price.toFixed(2)}</td>
            <td>${item.Quantity}</td>
            <td>$${item.SubTotal.toFixed(2)}</td>
          </tr>
        `;
      });

      ordersHtml += `
                </tbody>
              </table>
            </div>
            
            <div class="row">
              <div class="col-md-6">
                <h6>Shipping Address</h6>
                <p class="mb-1">${order.ShippingAddress.Address}</p>
                <p class="mb-1">${order.ShippingAddress.City}, ${order.ShippingAddress.State} ${order.ShippingAddress.ZipCode}</p>
                <p class="mb-1">Phone: ${order.ShippingAddress.Phone}</p>
              </div>
              <div class="col-md-6">
                <div class="d-flex justify-content-between mb-2">
                  <span>Subtotal:</span>
                  <span>$${order.SubTotal.toFixed(2)}</span>
                </div>
                <div class="d-flex justify-content-between mb-2">
                  <span>Shipping Fee:</span>
                  <span>$${order.ShippingFee.toFixed(2)}</span>
                </div>
                <hr>
                <div class="d-flex justify-content-between fw-bold">
                  <span>Total:</span>
                  <span>$${order.TotalAmount.toFixed(2)}</span>
                </div>
              </div>
            </div>
            
            <div class="mt-3 text-end">
              <a href="/Shop/OrderDetails?id=${order.id}" class="btn btn-primary">
                <i class="ti ti-eye me-1"></i> View Details
              </a>
              ${order.Status === 'Pending' ? `
                <button type="button" class="btn btn-danger cancel-order-btn" data-order-id="${order.id}">
                  <i class="ti ti-x me-1"></i> Cancel Order
                </button>
              ` : ''}
            </div>
          </div>
        </div>
      `;
    });

    orderHistoryContainer.innerHTML = ordersHtml;

    // Add event listeners to cancel buttons
    document.querySelectorAll('.cancel-order-btn').forEach(button => {
      button.addEventListener('click', function () {
        const orderId = this.getAttribute('data-order-id');
        cancelOrder(orderId);
      });
    });
  }

  // Get status class for order status
  function getStatusClass(status) {
    switch (status) {
      case 'Pending':
        return 'bg-label-warning';
      case 'Processing':
        return 'bg-label-info';
      case 'Shipped':
        return 'bg-label-primary';
      case 'Delivered':
        return 'bg-label-success';
      case 'Cancelled':
        return 'bg-label-danger';
      default:
        return 'bg-label-secondary';
    }
  }

  // Cancel order
  async function cancelOrder(orderId) {
    try {
      if (!confirm('Are you sure you want to cancel this order?')) {
        return;
      }

      // Update order status
      await ordersCollection.doc(orderId).update({
        Status: 'Cancelled',
        UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
      });

      // Restore product stock
      const orderDoc = await ordersCollection.doc(orderId).get();
      const order = orderDoc.data();

      for (const item of order.Items) {
        try {
          const productRef = productsCollection.doc(item.ProductId);
          const productDoc = await productRef.get();

          if (productDoc.exists) {
            const product = productDoc.data();
            const newStock = product.Stock + item.Quantity;

            await productRef.update({
              Stock: newStock,
              UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
            });
          }
        } catch (error) {
          console.error('Error restoring product stock:', error);
        }
      }

      toastr.success('Order cancelled successfully');

      // Refresh order history
      fetchOrderHistory();

    } catch (error) {
      console.error('Error cancelling order:', error);
      toastr.error('Failed to cancel order');
    }
  }

  // ===== ORDER CONFIRMATION FUNCTIONS =====

  // Fetch and display order confirmation
  async function fetchOrderConfirmation() {
    try {
      // Get order ID from URL
      const urlParams = new URLSearchParams(window.location.search);
      const orderId = urlParams.get('id');

      if (!orderId) {
        window.location.href = '/Shop';
        return;
      }

      // Get order details
      const orderDoc = await ordersCollection.doc(orderId).get();

      if (!orderDoc.exists) {
        toastr.error('Order not found');
        window.location.href = '/Shop';
        return;
      }

      const order = {
        id: orderDoc.id,
        ...orderDoc.data(),
        OrderDate: orderDoc.data().OrderDate?.toDate() || new Date()
      };

      // Render order confirmation
      renderOrderConfirmation(order);

    } catch (error) {
      console.error('Error fetching order confirmation:', error);
      toastr.error('Failed to load order confirmation');
    }
  }

  // Render order confirmation
  function renderOrderConfirmation(order) {
    const confirmationContainer = document.getElementById('order-confirmation-container');
    if (!confirmationContainer) return;

    const orderDate = new Date(order.OrderDate).toLocaleDateString();

    confirmationContainer.innerHTML = `
      <div class="card">
        <div class="card-body text-center py-5">
          <div class="mb-4">
            <i class="ti ti-circle-check text-success" style="font-size: 5rem;"></i>
          </div>
          <h2 class="mb-2">Thank You for Your Order!</h2>
          <p class="mb-1">Your order has been placed successfully.</p>
          <p class="mb-4">Order #${order.id}</p>
          
          <div class="row justify-content-center">
            <div class="col-md-8">
              <div class="alert alert-info mb-4">
                <h6 class="alert-heading mb-1">Order Details</h6>
                <p class="mb-0">We've sent a confirmation email to your registered email address with all the details.</p>
              </div>
              
              <div class="card mb-4">
                <div class="card-header">
                  <h5 class="mb-0">Order Summary</h5>
                </div>
                <div class="card-body">
                  <div class="mb-3">
                    <p class="mb-1"><strong>Order Date:</strong> ${orderDate}</p>
                    <p class="mb-1"><strong>Order Status:</strong> <span class="badge ${getStatusClass(order.Status)}">${order.Status}</span></p>
                    <p class="mb-1"><strong>Payment Method:</strong> ${order.PaymentMethod}</p>
                  </div>
                  
                  <div class="table-responsive mb-3">
                    <table class="table">
                      <thead>
                        <tr>
                          <th>Product</th>
                          <th>Price</th>
                          <th>Quantity</th>
                          <th>Subtotal</th>
                        </tr>
                      </thead>
                      <tbody>
    `;

    order.Items.forEach(item => {
      confirmationContainer.innerHTML += `
        <tr>
          <td>${item.Name}</td>
          <td>$${item.Price.toFixed(2)}</td>
          <td>${item.Quantity}</td>
          <td>$${item.SubTotal.toFixed(2)}</td>
        </tr>
      `;
    });

    confirmationContainer.innerHTML += `
                      </tbody>
                    </table>
                  </div>
                  
                  <div class="row">
                    <div class="col-md-6">
                      <h6>Shipping Address</h6>
                      <p class="mb-1">${order.ShippingAddress.Address}</p>
                      <p class="mb-1">${order.ShippingAddress.City}, ${order.ShippingAddress.State} ${order.ShippingAddress.ZipCode}</p>
                      <p class="mb-1">Phone: ${order.ShippingAddress.Phone}</p>
                    </div>
                    <div class="col-md-6">
                      <div class="d-flex justify-content-between mb-2">
                        <span>Subtotal:</span>
                        <span>$${order.SubTotal.toFixed(2)}</span>
                      </div>
                      <div class="d-flex justify-content-between mb-2">
                        <span>Shipping Fee:</span>
                        <span>$${order.ShippingFee.toFixed(2)}</span>
                      </div>
                      <hr>
                      <div class="d-flex justify-content-between fw-bold">
                        <span>Total:</span>
                        <span>$${order.TotalAmount.toFixed(2)}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              
              <div class="d-flex justify-content-center mt-4">
                <a href="/Shop" class="btn btn-primary me-2">
                  <i class="ti ti-shopping-cart me-1"></i> Continue Shopping
                </a>
                <a href="/Shop/OrderHistory" class="btn btn-outline-primary">
                  <i class="ti ti-list me-1"></i> View Order History
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  // ===== AFFILIATE FUNCTIONS =====

  // Generate affiliate link
  function generateAffiliateLink() {
    if (!userId || userRole !== '2') {
      return;
    }

    const baseUrl = window.location.origin;
    const affiliateLink = `${baseUrl}/Shop?ref=${userId}`;

    const affiliateLinkElement = document.getElementById('affiliate-link');
    if (affiliateLinkElement) {
      affiliateLinkElement.value = affiliateLink;
    }

    const copyLinkButton = document.getElementById('copy-affiliate-link');
    if (copyLinkButton) {
      copyLinkButton.addEventListener('click', function () {
        const linkInput = document.getElementById('affiliate-link');
        linkInput.select();
        document.execCommand('copy');

        toastr.success('Affiliate link copied to clipboard');
      });
    }
  }

  // Fetch affiliate dashboard data
  async function fetchAffiliateDashboard() {
    try {
      if (!userId || userRole !== '2') {
        window.location.href = '/Shop';
        return;
      }

      // Get affiliate data
      const userDoc = await usersCollection.doc(userId).get();

      if (!userDoc.exists) {
        toastr.error('User not found');
        return;
      }

      const userData = userDoc.data();
      const customerIds = userData.CustomerId || [];
      const orderRefs = userData.OrderId || [];

      // Fetch orders
      const orders = [];
      let totalRevenue = 0;
      let totalCommission = 0;

      for (const orderRef of orderRefs) {
        try {
          const orderDoc = await db.doc(orderRef.path).get();

          if (orderDoc.exists) {
            const orderData = orderDoc.data();

            // Only count completed orders
            if (orderData.Status !== 'Cancelled') {
              orders.push({
                id: orderDoc.id,
                ...orderData,
                OrderDate: orderData.OrderDate?.toDate() || new Date()
              });

              totalRevenue += orderData.TotalAmount || 0;
              totalCommission += orderData.AffiliateCommission || 0;
            }
          }
        } catch (error) {
          console.error('Error fetching order:', error);
        }
      }

      // Sort orders by date (newest first)
      orders.sort((a, b) => b.OrderDate - a.OrderDate);

      // Render affiliate dashboard
      renderAffiliateDashboard({
        customersCount: customerIds.length,
        ordersCount: orders.length,
        totalRevenue,
        totalCommission,
        recentOrders: orders.slice(0, 5) // Get 5 most recent orders
      });

      // Generate affiliate link
      generateAffiliateLink();

    } catch (error) {
      console.error('Error fetching affiliate dashboard:', error);
      toastr.error('Failed to load affiliate dashboard');
    }
  }
  // Render affiliate dashboard
  function renderAffiliateDashboard(data) {
    // Render stats cards
    const statsContainer = document.getElementById('affiliate-stats-container');
    if (statsContainer) {
      statsContainer.innerHTML = `
        <div class="row">
          <div class="col-lg-3 col-md-6 col-sm-6 mb-4">
            <div class="card">
              <div class="card-body">
                <div class="d-flex justify-content-between">
                  <div class="card-info">
                    <p class="card-text">Customers</p>
                    <div class="d-flex align-items-end mb-2">
                      <h4 class="card-title mb-0 me-2">${data.customersCount}</h4>
                    </div>
                  </div>
                  <div class="card-icon">
                    <span class="badge bg-label-primary rounded p-2">
                      <i class="ti ti-users ti-sm"></i>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          
          <div class="col-lg-3 col-md-6 col-sm-6 mb-4">
            <div class="card">
              <div class="card-body">
                <div class="d-flex justify-content-between">
                  <div class="card-info">
                    <p class="card-text">Orders</p>
                    <div class="d-flex align-items-end mb-2">
                      <h4 class="card-title mb-0 me-2">${data.ordersCount}</h4>
                    </div>
                  </div>
                  <div class="card-icon">
                    <span class="badge bg-label-info rounded p-2">
                      <i class="ti ti-shopping-cart ti-sm"></i>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          
          <div class="col-lg-3 col-md-6 col-sm-6 mb-4">
            <div class="card">
              <div class="card-body">
                <div class="d-flex justify-content-between">
                  <div class="card-info">
                    <p class="card-text">Revenue</p>
                    <div class="d-flex align-items-end mb-2">
                      <h4 class="card-title mb-0 me-2">$${data.totalRevenue.toFixed(2)}</h4>
                    </div>
                  </div>
                  <div class="card-icon">
                    <span class="badge bg-label-success rounded p-2">
                      <i class="ti ti-currency-dollar ti-sm"></i>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          
          <div class="col-lg-3 col-md-6 col-sm-6 mb-4">
            <div class="card">
              <div class="card-body">
                <div class="d-flex justify-content-between">
                  <div class="card-info">
                    <p class="card-text">Commission</p>
                    <div class="d-flex align-items-end mb-2">
                      <h4 class="card-title mb-0 me-2">$${data.totalCommission.toFixed(2)}</h4>
                    </div>
                  </div>
                  <div class="card-icon">
                    <span class="badge bg-label-warning rounded p-2">
                      <i class="ti ti-cash ti-sm"></i>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      `;
    }

    // Render affiliate link
    const linkContainer = document.getElementById('affiliate-link-container');
    if (linkContainer) {
      const baseUrl = window.location.origin;
      const affiliateLink = `${baseUrl}/Shop?ref=${userId}`;

      linkContainer.innerHTML = `
        <div class="card mb-4">
          <div class="card-header">
            <h5 class="mb-0">Your Affiliate Link</h5>
          </div>
          <div class="card-body">
            <p class="mb-3">Share this link with your audience to earn commissions on their purchases.</p>
            <div class="input-group">
              <input type="text" class="form-control" id="affiliate-link" value="${affiliateLink}" readonly>
              <button class="btn btn-primary" type="button" id="copy-affiliate-link">
                <i class="ti ti-copy me-1"></i> Copy
              </button>
            </div>
          </div>
        </div>
      `;

      // Add event listener to copy button
      document.getElementById('copy-affiliate-link').addEventListener('click', function () {
        const linkInput = document.getElementById('affiliate-link');
        linkInput.select();
        document.execCommand('copy');

        toastr.success('Affiliate link copied to clipboard');
      });
    }

    // Render recent orders
    const recentOrdersContainer = document.getElementById('recent-orders-container');
    if (recentOrdersContainer) {
      if (data.recentOrders.length === 0) {
        recentOrdersContainer.innerHTML = `
          <div class="card">
            <div class="card-header">
              <h5 class="mb-0">Recent Orders</h5>
            </div>
            <div class="card-body text-center py-5">
              <i class="ti ti-shopping-cart text-primary" style="font-size: 3rem;"></i>
              <h3 class="mt-3">No Orders Yet</h3>
              <p class="mb-0">Share your affiliate link to start earning commissions.</p>
            </div>
          </div>
        `;
        return;
      }

      let ordersHtml = `
        <div class="card">
          <div class="card-header">
            <h5 class="mb-0">Recent Orders</h5>
          </div>
          <div class="table-responsive">
            <table class="table">
              <thead>
                <tr>
                  <th>Order ID</th>
                  <th>Date</th>
                  <th>Amount</th>
                  <th>Commission</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
      `;

      data.recentOrders.forEach(order => {
        const orderDate = new Date(order.OrderDate).toLocaleDateString();
        const statusClass = getStatusClass(order.Status);

        ordersHtml += `
          <tr>
            <td><a href="/Shop/OrderDetails?id=${order.id}" class="fw-medium">#${order.id.substring(0, 8)}</a></td>
            <td>${orderDate}</td>
            <td>$${order.TotalAmount.toFixed(2)}</td>
            <td>$${(order.AffiliateCommission || 0).toFixed(2)}</td>
            <td><span class="badge ${statusClass}">${order.Status}</span></td>
          </tr>
        `;
      });

      ordersHtml += `
              </tbody>
            </table>
          </div>
          <div class="card-footer text-end">
            <a href="/Shop/AffiliateOrders" class="btn btn-primary">View All Orders</a>
          </div>
        </div>
      `;

      recentOrdersContainer.innerHTML = ordersHtml;
    }
  }

  // Fetch affiliate orders
  async function fetchAffiliateOrders() {
    try {
      if (!userId || userRole !== '2') {
        window.location.href = '/Shop';
        return;
      }

      // Get affiliate data
      const userDoc = await usersCollection.doc(userId).get();

      if (!userDoc.exists) {
        toastr.error('User not found');
        return;
      }

      const userData = userDoc.data();
      const orderRefs = userData.OrderId || [];

      // Fetch orders
      const orders = [];

      for (const orderRef of orderRefs) {
        try {
          const orderDoc = await db.doc(orderRef.path).get();

          if (orderDoc.exists) {
            const orderData = orderDoc.data();

            orders.push({
              id: orderDoc.id,
              ...orderData,
              OrderDate: orderData.OrderDate?.toDate() || new Date()
            });
          }
        } catch (error) {
          console.error('Error fetching order:', error);
        }
      }

      // Sort orders by date (newest first)
      orders.sort((a, b) => b.OrderDate - a.OrderDate);

      // Render affiliate orders
      renderAffiliateOrders(orders);

    } catch (error) {
      console.error('Error fetching affiliate orders:', error);
      toastr.error('Failed to load affiliate orders');
    }
  }

  // Render affiliate orders
  function renderAffiliateOrders(orders) {
    const ordersContainer = document.getElementById('affiliate-orders-container');
    if (!ordersContainer) return;

    if (orders.length === 0) {
      ordersContainer.innerHTML = `
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="ti ti-shopping-cart text-primary" style="font-size: 4rem;"></i>
            <h3 class="mt-4">No Orders Yet</h3>
            <p class="mb-4">Share your affiliate link to start earning commissions.</p>
            <a href="/Shop/AffiliateDashboard" class="btn btn-primary">Back to Dashboard</a>
          </div>
        </div>
      `;
      return;
    }

    let ordersHtml = `
      <div class="card">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="mb-0">All Orders</h5>
          <div class="d-flex">
            <input type="text" class="form-control me-2" id="order-search" placeholder="Search orders...">
            <select class="form-select" id="order-status-filter">
              <option value="">All Statuses</option>
              <option value="Pending">Pending</option>
              <option value="Processing">Processing</option>
              <option value="Shipped">Shipped</option>
              <option value="Delivered">Delivered</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>
        </div>
        <div class="table-responsive">
          <table class="table" id="affiliate-orders-table">
            <thead>
              <tr>
                <th>Order ID</th>
                <th>Date</th>
                <th>Customer</th>
                <th>Items</th>
                <th>Amount</th>
                <th>Commission</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
    `;

    orders.forEach(order => {
      const orderDate = new Date(order.OrderDate).toLocaleDateString();
      const statusClass = getStatusClass(order.Status);
      const itemsCount = order.Items ? order.Items.length : 0;

      ordersHtml += `
        <tr data-order-id="${order.id}" data-order-status="${order.Status}">
          <td><span class="fw-medium">#${order.id.substring(0, 8)}</span></td>
          <td>${orderDate}</td>
          <td>${order.CustomerId ? order.CustomerId.substring(0, 8) + '...' : 'Guest'}</td>
          <td>${itemsCount} item${itemsCount !== 1 ? 's' : ''}</td>
          <td>$${order.TotalAmount.toFixed(2)}</td>
          <td>$${(order.AffiliateCommission || 0).toFixed(2)}</td>
          <td><span class="badge ${statusClass}">${order.Status}</span></td>
          <td>
            <div class="dropdown">
              <button type="button" class="btn btn-sm dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                <i class="ti ti-dots-vertical"></i>
              </button>
              <div class="dropdown-menu">
                <a class="dropdown-item" href="/Shop/OrderDetails?id=${order.id}">
                  <i class="ti ti-eye me-1"></i> View Details
                </a>
              </div>
            </div>
          </td>
        </tr>
      `;
    });

    ordersHtml += `
            </tbody>
          </table>
        </div>
      </div>
    `;

    ordersContainer.innerHTML = ordersHtml;

    // Add event listeners for search and filter
    document.getElementById('order-search').addEventListener('input', function () {
      filterAffiliateOrders();
    });

    document.getElementById('order-status-filter').addEventListener('change', function () {
      filterAffiliateOrders();
    });
  }

  // Filter affiliate orders
  function filterAffiliateOrders() {
    const searchTerm = document.getElementById('order-search').value.toLowerCase();
    const statusFilter = document.getElementById('order-status-filter').value;

    const rows = document.querySelectorAll('#affiliate-orders-table tbody tr');

    rows.forEach(row => {
      const orderId = row.getAttribute('data-order-id').toLowerCase();
      const orderStatus = row.getAttribute('data-order-status');

      const matchesSearch = orderId.includes(searchTerm);
      const matchesStatus = statusFilter === '' || orderStatus === statusFilter;

      if (matchesSearch && matchesStatus) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // ===== INITIALIZATION =====

  // Initialize page based on current URL
  function initPage() {
    // Get user info
    initUserInfo();

    // Update cart count
    updateCartCount();

    // Get current page
    const currentPath = window.location.pathname;

    // Initialize page-specific functionality
    if (currentPath === '/Shop' || currentPath === '/Shop/Index') {
      // Shop index page
      document.addEventListener('DOMContentLoaded', function () {
        fetchProducts().then(renderProducts);

        // Add event listeners for filters
        document.getElementById('searchQuery')?.addEventListener('input', function () {
          document.getElementById('minPriceDisplay').textContent = document.getElementById('minPrice').value;
          document.getElementById('maxPriceDisplay').textContent = document.getElementById('maxPrice').value;
        });

        document.getElementById('minPrice')?.addEventListener('input', function () {
          document.getElementById('minPriceDisplay').textContent = this.value;
        });

        document.getElementById('maxPrice')?.addEventListener('input', function () {
          document.getElementById('maxPriceDisplay').textContent = this.value;
        });

        // Check for affiliate referral in URL
        const urlParams = new URLSearchParams(window.location.search);
        const affiliateRef = urlParams.get('ref');

        if (affiliateRef) {
          // Store affiliate ID in session storage
          sessionStorage.setItem('affiliateRef', affiliateRef);
          console.log('Affiliate referral detected:', affiliateRef);
        }
      });
    } else if (currentPath.includes('/Shop/ProductDetails')) {
      // Product details page
      document.addEventListener('DOMContentLoaded', function () {
        const urlParams = new URLSearchParams(window.location.search);
        const productId = urlParams.get('id');

        if (productId) {
          fetchProductDetails(productId).then(renderProductDetails);
        } else {
          window.location.href = '/Shop';
        }
      });
    } else if (currentPath === '/Shop/Cart') {
      // Cart page
      document.addEventListener('DOMContentLoaded', function () {
        fetchAndRenderCart();
      });
    } else if (currentPath === '/Shop/Checkout') {
      // Checkout page
      document.addEventListener('DOMContentLoaded', function () {
        initCheckout();
      });
    } else if (currentPath === '/Shop/OrderConfirmation') {
      // Order confirmation page
      document.addEventListener('DOMContentLoaded', function () {
        fetchOrderConfirmation();
      });
    } else if (currentPath === '/Shop/OrderHistory') {
      // Order history page
      document.addEventListener('DOMContentLoaded', function () {
        fetchOrderHistory();
      });
    } else if (currentPath === '/Shop/AffiliateDashboard') {
      // Affiliate dashboard page
      document.addEventListener('DOMContentLoaded', function () {
        fetchAffiliateDashboard();
      });
    } else if (currentPath === '/Shop/AffiliateOrders') {
      // Affiliate orders page
      document.addEventListener('DOMContentLoaded', function () {
        fetchAffiliateOrders();
      });
    }
  }

  // Initialize the page
  initPage();
});





