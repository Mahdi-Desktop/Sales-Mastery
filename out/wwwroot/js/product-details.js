document.addEventListener('DOMContentLoaded', function () {
  // Initialize Firestore
  const db = firebase.firestore();

  // Get product ID from URL
  const urlParams = new URLSearchParams(window.location.search);
  const productId = urlParams.get('id');

  if (!productId) {
    window.location.href = '/Shop/Index';
    return;
  }

  // DOM Elements
  const productContainer = document.getElementById('productContainer');
  const relatedProductsContainer = document.getElementById('relatedProductsContainer');
  const quantityInput = document.getElementById('quantity');
  const decreaseQtyBtn = document.getElementById('decreaseQty');
  const increaseQtyBtn = document.getElementById('increaseQty');
  const addToCartBtn = document.getElementById('addToCartBtn');

  // Load product details
  function loadProductDetails() {
    db.collection('products')
      .doc(productId)
      .get()
      .then(doc => {
        if (!doc.exists) {
          productContainer.innerHTML = `
                        <div class="col-12">
                            <div class="card">
                                <div class="card-body text-center py-5">
                                    <i class="ti ti-alert-triangle text-danger" style="font-size: 3rem;"></i>
                                    <h3 class="mt-3">Product Not Found</h3>
                                    <p class="mb-3">The product you are looking for does not exist.</p>
                                    <a href="/Shop/Index" class="btn btn-primary">Back to Shop</a>
                                </div>
                            </div>
                        </div>
                    `;
          return;
        }

        const product = doc.data();

        // Update document title
        document.title = product.Name + ' - Sales Mastery';

        // Format data
        const hasDiscount = product.Discount > 0;
        const discountPrice = hasDiscount ? product.Price - (product.Price * product.Discount) / 100 : product.Price;
        const discountPercentage = Math.round(product.Discount);
        const imageUrl = product.Image && product.Image.length > 0 ? product.Image[0] : '/img/products/default.jpg';
        const stockStatus =
          product.Stock > 10 ? 'In Stock' : product.Stock > 0 ? `Low Stock: ${product.Stock} left` : 'Out of Stock';
        const stockStatusClass = product.Stock > 10 ? 'success' : product.Stock > 0 ? 'warning' : 'danger';

        // Render product details
        productContainer.innerHTML = `
                    <div class="row">
                        <!-- Product Image -->
                        <div class="col-lg-5 col-md-6 mb-4 mb-md-0">
                            <div class="card">
                                <div class="card-body text-center p-5">
                                    <img src="${imageUrl}" class="img-fluid" alt="${product.Name}" style="max-height: 400px;">
                                </div>
                            </div>
                        </div>

                        <!-- Product Details -->
                        <div class="col-lg-7 col-md-6">
                            <h3 class="mb-2">${product.Name}</h3>

                            <!-- Price -->
                            <div class="d-flex align-items-center mb-3">
                                ${
                                  hasDiscount
                                    ? `<h4 class="text-danger mb-0 me-2">$${discountPrice.toFixed(2)}</h4>
                                     <h5 class="text-muted mb-0 me-2"><del>$${product.Price.toFixed(2)}</del></h5>
                                     <span class="badge bg-danger">-${discountPercentage}%</span>`
                                    : `<h4 class="mb-0">$${product.Price.toFixed(2)}</h4>`
                                }
                            </div>

                            <!-- Availability -->
                            <div class="mb-4">
                                <span class="badge bg-label-${stockStatusClass} me-1">${stockStatus}</span>
                            </div>

                            <!-- Description -->
                            <div class="mb-4">
                                <h5>Description</h5>
                                <p>${product.Description}</p>
                            </div>

                            <!-- Categories & Brand -->
                            <div class="mb-4" id="productMetadata">
                                <!-- Will be populated with categories and brand -->
                            </div>

                            <!-- Quantity Selection & Add to Cart -->
                            <form id="addToCartForm" class="mb-4">
                                <div class="row g-3">
                                    <div class="col-md-4">
                                        <label for="quantity" class="form-label">Quantity</label>
                                        <div class="input-group">
                                            <button type="button" class="btn btn-outline-primary" id="decreaseQty">
                                                <i class="ti ti-minus"></i>
                                            </button>
                                            <input type="number" id="quantity" name="quantity" class="form-control text-center"
                                                   value="1" min="1" max="${product.Stock}"
                                                   ${product.Stock <= 0 ? 'disabled' : ''}>
                                            <button type="button" class="btn btn-outline-primary" id="increaseQty">
                                                <i class="ti ti-plus"></i>
                                            </button>
                                        </div>
                                    </div>
                                    <div class="col-md-8 d-flex align-items-end">
                                        <button type="button" id="addToCartBtn" class="btn btn-primary w-100"
                                                ${product.Stock <= 0 ? 'disabled' : ''}>
                                            <i class="ti ti-shopping-cart me-1"></i> Add to Cart
                                        </button>
                                    </div>
                                </div>
                            </form>

                            <!-- Back to shop button -->
                            <div>
                                <a href="/Shop/Index" class="btn btn-outline-secondary">
                                    <i class="ti ti-arrow-left me-1"></i> Back to Shop
                                </a>
                                <a href="/Shop/Cart" class="btn btn-outline-primary ms-2">
                                    <i class="ti ti-shopping-cart me-1"></i> View Cart <span class="cart-count">0</span>
                                </a>
                            </div>
                        </div>
                    </div>
                `;

        // Setup event listeners for the newly added elements
        setupProductEventListeners(product);

        // Load category and brand info
        loadProductMetadata(product);

        // Load related products
        loadRelatedProducts(product);
      })
      .catch(error => {
        console.error('Error loading product: ', error);
        productContainer.innerHTML = `
                    <div class="col-12">
                        <div class="card">
                            <div class="card-body text-center py-5">
                                <i class="ti ti-alert-triangle text-danger" style="font-size: 3rem;"></i>
                                <h3 class="mt-3">Error Loading Product</h3>
                                <p class="mb-3">An error occurred while loading the product details. Please try again later.</p>
                                <a href="/Shop/Index" class="btn btn-primary">Back to Shop</a>
                            </div>
                        </div>
                    </div>
                `;
      });
  }

  // Load product metadata (categories, brand)
  function loadProductMetadata(product) {
    const metadataContainer = document.getElementById('productMetadata');
    let promises = [];
    let categoryData = null;
    let brandData = null;

    // Get category data
    if (product.CategoryId) {
      const categoryId = product.CategoryId.split('/')[1];
      promises.push(
        db
          .collection('categories')
          .doc(categoryId)
          .get()
          .then(doc => {
            if (doc.exists) {
              categoryData = doc.data();
              categoryData.id = doc.id;
            }
          })
      );
    }

    // Get brand data
    if (product.BrandId) {
      const brandId = product.BrandId.split('/')[1];
      promises.push(
        db
          .collection('brands')
          .doc(brandId)
          .get()
          .then(doc => {
            if (doc.exists) {
              brandData = doc.data();
              brandData.id = doc.id;
            }
          })
      );
    }

    Promise.all(promises)
      .then(() => {
        let html = '';

        // Add category info if available
        if (categoryData) {
          html += `
                        <div class="mb-2">
                            <h5 class="mb-2">Category</h5>
                            <a href="/Shop/Index?category=${categoryData.id}" class="badge bg-primary me-1">${categoryData.Name}</a>
                        </div>
                    `;
        }

        // Add brand info if available
        if (brandData) {
          html += `
                        <div>
                            <h5 class="mb-2">Brand</h5>
                            <span class="badge bg-secondary me-1">${brandData.Name}</span>
                        </div>
                    `;
        }

        metadataContainer.innerHTML = html;
      })
      .catch(error => {
        console.error('Error loading product metadata: ', error);
      });
  }

  // Load related products
  function loadRelatedProducts(product) {
    if (!product.CategoryId) return;

    const categoryId = product.CategoryId.split('/')[1];

    db.collection('products')
      .where('CategoryId', '==', `categories/${categoryId}`)
      .where('ProductId', '!=', product.ProductId)
      .limit(4)
      .get()
      .then(snapshot => {
        if (snapshot.empty) {
          relatedProductsContainer.closest('.row').style.display = 'none';
          return;
        }

        let html = '';

        snapshot.forEach(doc => {
          const relatedProduct = doc.data();
          const hasDiscount = relatedProduct.Discount > 0;
          const discountPrice = hasDiscount
            ? relatedProduct.Price - (relatedProduct.Price * relatedProduct.Discount) / 100
            : relatedProduct.Price;
          const discountPercentage = Math.round(relatedProduct.Discount);
          const imageUrl =
            relatedProduct.Image && relatedProduct.Image.length > 0
              ? relatedProduct.Image[0]
              : '/img/products/default.jpg';

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
                                            ${
                                              hasDiscount
                                                ? `<span class="text-muted text-decoration-line-through me-1">$${relatedProduct.Price.toFixed(2)}</span>
                                                 <span class="fw-bold text-danger">$${discountPrice.toFixed(2)}</span>`
                                                : `<span class="fw-bold">$${relatedProduct.Price.toFixed(2)}</span>`
                                            }
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
        console.error('Error loading related products: ', error);
        relatedProductsContainer.closest('.row').style.display = 'none';
      });
  }

  // Setup event listeners for product details page
  function setupProductEventListeners(product) {
    const quantityInput = document.getElementById('quantity');
    const decreaseQtyBtn = document.getElementById('decreaseQty');
    const increaseQtyBtn = document.getElementById('increaseQty');
    const addToCartBtn = document.getElementById('addToCartBtn');

    // Quantity controls
    decreaseQtyBtn.addEventListener('click', () => {
      const currentQty = parseInt(quantityInput.value);
      if (currentQty > 1) {
        quantityInput.value = currentQty - 1;
      }
    });

    increaseQtyBtn.addEventListener('click', () => {
      const currentQty = parseInt(quantityInput.value);
      quantityInput.value = currentQty + 1;
    });

    // Add to cart button
    addToCartBtn.addEventListener('click', () => {
      const quantity = parseInt(quantityInput.value);
      addToCart(product, quantity);
    });
  }

  async function addToCart(product, quantity) {
    try {
      // Get user ID
      const userId = getStoredUserId();
      if (!userId) {
        window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname);
        return;
      }

      // Get user's cart
      const cartRef = db.collection('carts').doc(userId);
      const cartDoc = await cartRef.get();

      let cartItems = [];
      if (cartDoc.exists) {
        cartItems = cartDoc.data().items || [];
      }

      // Check if product already exists in cart
      const existingItemIndex = cartItems.findIndex(item => item.ProductId === product.Id);

      if (existingItemIndex !== -1) {
        // Update quantity if product exists
        cartItems[existingItemIndex].Quantity += quantity;
      } else {
        // Add new item if product doesn't exist
        cartItems.push({
          ProductId: product.Id,
          Name: product.Name,
          ImageUrl: product.ImageUrl || '/img/products/default.jpg',
          Price: product.Discount > 0 ? product.Price - (product.Price * product.Discount) / 100 : product.Price,
          Quantity: quantity
        });
      }

      // Update cart in Firestore
      await cartRef.set({
        items: cartItems,
        updatedAt: firebase.firestore.FieldValue.serverTimestamp()
      });

      // Show success message
      Swal.fire({
        icon: 'success',
        title: 'Added to Cart!',
        text: `${product.Name} has been added to your cart.`,
        showConfirmButton: false,
        timer: 1500
      });

      // Update cart count
      updateCartCount();
    } catch (error) {
      console.error('Error adding to cart:', error);
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'There was an error adding the product to your cart. Please try again.'
      });
    }
  }

  function getStoredUserId() {
    return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
  }

  function updateCartCount() {
    const userId = getStoredUserId();
    if (!userId) return;

    db.collection('carts')
      .doc(userId)
      .get()
      .then(doc => {
        if (doc.exists) {
          const cartItems = doc.data().items || [];
          const totalItems = cartItems.reduce((sum, item) => sum + item.Quantity, 0);
          const cartCountElement = document.getElementById('cart-item-count');
          if (cartCountElement) {
            cartCountElement.textContent = totalItems;
          }
        }
      })
      .catch(error => console.error('Error updating cart count:', error));
  }

  // Initialize
  loadProductDetails();
});
