document.addEventListener('DOMContentLoaded', function () {
  console.log("Cart.js loaded");

  // Wait for Firebase to be initialized
  if (typeof firebase !== 'undefined' && firebase.apps.length > 0) {
    const db = firebase.firestore();
    initializeCart(db);
  } else {
    console.log("Waiting for Firebase to initialize...");
    document.addEventListener('firebase-ready', function (e) {
      console.log("Firebase ready event received");
      const db = e.detail.db || firebase.firestore();
      initializeCart(db);
    });
  }

  function initializeCart(db) {
    console.log("Initializing cart with db:", db);

    // Get user ID
    const userId = getStoredUserId();
    console.log("Cart.js - User ID:", userId);

    if (!userId) {
      window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname);
      return;
    }

    // DOM Elements
    const cartItemsContainer = document.getElementById('cartItems');
    const subtotalElement = document.getElementById('subtotal');
    const totalElement = document.getElementById('total');
    const emptyCartMessage = document.getElementById('emptyCartMessage');
    const cartContent = document.getElementById('cartContent');

    // Load cart
    loadCart();

    function loadCart() {
      console.log("Loading cart for user:", userId);

      db.collection('carts').doc(userId).get()
        .then(doc => {
          console.log("Cart document exists:", doc.exists);

          if (!doc.exists || !doc.data().items || doc.data().items.length === 0) {
            showEmptyCart();
            return;
          }

          const cart = doc.data();
          console.log("Cart data:", cart);

          renderCart(cart.items);
        })
        .catch(error => {
          console.error("Error loading cart:", error);
          if (cartItemsContainer) {
            cartItemsContainer.innerHTML = `
              <tr>
                <td colspan="5" class="text-center py-4">
                  <i class="ti ti-alert-triangle text-danger mb-2" style="font-size: 2rem;"></i>
                  <p>An error occurred while loading your cart. Please try again later.</p>
                  <p class="text-muted small">Error: ${error.message}</p>
                </td>
              </tr>
            `;
          }
        });
    }

    function showEmptyCart() {
      console.log("Showing empty cart message");
      if (emptyCartMessage) emptyCartMessage.style.display = 'block';
      if (cartContent) cartContent.style.display = 'none';
    }

    function renderCart(items) {
      console.log("Rendering cart items:", items);

      if (!cartItemsContainer) {
        console.error("Cart items container not found");
        return;
      }

      let html = '';
      let subtotal = 0;

      items.forEach(item => {
        const price = item.price || 0;
        const discount = item.discount || 0;
        const effectivePrice = discount > 0
          ? price - (price * discount / 100)
          : price;

        const itemSubtotal = effectivePrice * item.quantity;
        subtotal += itemSubtotal;

        html += `
          <tr>
            <td>
              <div class="d-flex align-items-center">
                <div class="avatar avatar-lg me-3">
                  <img src="${item.imageUrl || '/img/products/default.jpg'}" alt="${item.name}" class="rounded">
                </div>
                <div>
                  <h6 class="mb-0">${item.name}</h6>
                  <small class="text-muted">
                    ${discount > 0
            ? `<span class="text-decoration-line-through me-1">$${price.toFixed(2)}</span> 
                         <span class="text-danger">$${effectivePrice.toFixed(2)}</span>`
            : `$${price.toFixed(2)}`}
                  </small>
                </div>
              </div>
            </td>
            <td>$${effectivePrice.toFixed(2)}</td>
            <td>
              <div class="input-group input-group-sm" style="width: 120px;">
                <button class="btn btn-outline-secondary quantity-decrease" data-product-id="${item.productId}">
                  <i class="ti ti-minus"></i>
                </button>
                <input type="number" class="form-control text-center quantity-input" 
                       value="${item.quantity}" min="1" max="100" 
                       data-product-id="${item.productId}">
                <button class="btn btn-outline-secondary quantity-increase" data-product-id="${item.productId}">
                  <i class="ti ti-plus"></i>
                </button>
              </div>
            </td>
            <td class="text-end">$${itemSubtotal.toFixed(2)}</td>
            <td class="text-center">
              <button class="btn btn-sm btn-outline-danger remove-item" data-product-id="${item.productId}">
                <i class="ti ti-trash"></i>
              </button>
            </td>
          </tr>
        `;
      });

      cartItemsContainer.innerHTML = html;

      // Update totals
      const shipping = 4; // $4 shipping fee
      const total = subtotal + shipping;

      if (subtotalElement) subtotalElement.textContent = subtotal.toFixed(2);
      if (totalElement) totalElement.textContent = total.toFixed(2);

      // Show cart content
      if (emptyCartMessage) emptyCartMessage.style.display = 'none';
      if (cartContent) cartContent.style.display = 'block';

      // Add event listeners to cart item controls
      addCartItemEventListeners();
    }

    function addCartItemEventListeners() {
      // Quantity increase buttons
      document.querySelectorAll('.quantity-increase').forEach(button => {
        button.addEventListener('click', function () {
          const productId = this.getAttribute('data-product-id');
          const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
          input.value = parseInt(input.value) + 1;
          updateCartItemQuantity(productId, parseInt(input.value));
        });
      });

      // Quantity decrease buttons
      document.querySelectorAll('.quantity-decrease').forEach(button => {
        button.addEventListener('click', function () {
          const productId = this.getAttribute('data-product-id');
          const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
          if (parseInt(input.value) > 1) {
            input.value = parseInt(input.value) - 1;
            updateCartItemQuantity(productId, parseInt(input.value));
          }
        });
      });

      // Quantity input changes
      document.querySelectorAll('.quantity-input').forEach(input => {
        input.addEventListener('change', function () {
          const productId = this.getAttribute('data-product-id');
          let value = parseInt(this.value);
          if (isNaN(value) || value < 1) value = 1;
          this.value = value;
          updateCartItemQuantity(productId, value);
        });
      });

      // Remove item buttons
      document.querySelectorAll('.remove-item').forEach(button => {
        button.addEventListener('click', function () {
          const productId = this.getAttribute('data-product-id');
          removeCartItem(productId);
        });
      });
    }

    function updateCartItemQuantity(productId, quantity) {
      db.collection('carts').doc(userId).get()
        .then(doc => {
          if (!doc.exists) return;

          const cart = doc.data();
          const itemIndex = cart.items.findIndex(item => item.productId === productId);

          if (itemIndex !== -1) {
            cart.items[itemIndex].quantity = quantity;
            cart.items[itemIndex].subtotal = calculateSubtotal(
              cart.items[itemIndex].price,
              cart.items[itemIndex].discount || 0,
              quantity
            );

            return db.collection('carts').doc(userId).update({
              items: cart.items,
              updatedAt: firebase.firestore.FieldValue.serverTimestamp()
            });
          }
        })
        .then(() => {
          loadCart(); // Reload cart to show updated quantities
        })
        .catch(error => {
          console.error("Error updating quantity:", error);
        });
    }

    function removeCartItem(productId) {
      db.collection('carts').doc(userId).get()
        .then(doc => {
          if (!doc.exists) return;

          const cart = doc.data();
          const updatedItems = cart.items.filter(item => item.productId !== productId);

          return db.collection('carts').doc(userId).update({
            items: updatedItems,
            updatedAt: firebase.firestore.FieldValue.serverTimestamp()
          });
        })
        .then(() => {
          loadCart(); // Reload cart after removing item
          updateCartCount(); // Update the cart count in the header
        })
        .catch(error => {
          console.error("Error removing item:", error);
        });
    }
  }

  // Get user ID from storage
  function getStoredUserId() {
    try {
      const userDataScript = document.getElementById('user-data');
      if (userDataScript) {
        const userData = JSON.parse(userDataScript.textContent);
        if (userData && userData.userId) {
          return userData.userId;
        }
      }
    } catch (e) {
      console.error('Error parsing user data:', e);
    }

    return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
  }

  // Calculate subtotal with discount
  function calculateSubtotal(price, discount, quantity) {
    const discountedPrice = discount > 0 ? price - (price * discount / 100) : price;
    return discountedPrice * quantity;
  }

  // Update cart count in the header
  function updateCartCount() {
    if (typeof firebase === 'undefined' || !firebase.apps.length) {
      console.error("Firebase not initialized for updateCartCount");
      return;
    }

    const userId = getStoredUserId();
    if (!userId) return;

    const db = firebase.firestore();

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
        console.error("Error updating cart count:", error);
      });
  }

  // Add clear cart functionality
  function clearCart() {
    const userId = getStoredUserId();
    if (!userId) return;

    if (typeof firebase === 'undefined' || !firebase.apps.length) {
      console.error("Firebase not initialized for clearCart");
      return;
    }

    const db = firebase.firestore();

    db.collection('carts')
      .doc(userId)
      .set({
        items: [],
        userId: userId,
        updatedAt: firebase.firestore.FieldValue.serverTimestamp()
      })
      .then(() => {
        console.log("Cart cleared successfully");

        // Show empty cart message
        const emptyCartMessage = document.getElementById('emptyCartMessage');
        const cartContent = document.getElementById('cartContent');

        if (emptyCartMessage) emptyCartMessage.style.display = 'block';
        if (cartContent) cartContent.style.display = 'none';

        // Update cart count
        updateCartCount();
      })
      .catch(error => {
        console.error("Error clearing cart:", error);
        alert("An error occurred while clearing your cart");
      });
  }

  // Initialize clear cart button
  const clearCartButton = document.getElementById('clearCartButton');
  if (clearCartButton) {
    clearCartButton.addEventListener('click', function () {
      if (confirm('Are you sure you want to clear your cart?')) {
        clearCart();
      }
    });
  }

  // Initialize checkout button
/*  const checkoutButton = document.getElementById('checkoutButton');
  if (checkoutButton) {
    checkoutButton.addEventListener('click', function (e) {
      const userId = getStoredUserId();
      if (!userId) {
        e.preventDefault();
        window.location.href = '/Auth/LoginBasic?returnUrl=/Shop/Checkout';
        return;
      }

      // Check if cart is empty before proceeding to checkout
      if (typeof firebase === 'undefined' || !firebase.apps.length) {
        console.error("Firebase not initialized for checkout");
        e.preventDefault();
        alert("Please wait a moment and try again. The system is still initializing.");
        return;
      }

      const db = firebase.firestore();

      db.collection('carts')
        .doc(userId)
        .get()
        .then(doc => {
          if (!doc.exists || !doc.data().items || doc.data().items.length === 0) {
            e.preventDefault();
            alert("Your cart is empty. Please add items to your cart before checkout.");
          }
        })
        .catch(error => {
          console.error("Error checking cart for checkout:", error);
          e.preventDefault();
          alert("An error occurred while processing your request. Please try again.");
        });
    });
  }

*/
  /*// Replace the checkout button event listener with this simpler version
  const checkoutButton = document.getElementById('checkoutButton');
  if (checkoutButton) {
    checkoutButton.addEventListener('click', function (e) {
      e.preventDefault(); // Prevent default navigation

      const userId = getStoredUserId();
      if (!userId) {
        window.location.href = '/Auth/LoginBasic?returnUrl=/Shop/Checkout';
        return;
      }

      console.log("Navigating to checkout page...");
      window.location.href = '/Shop/Checkout';
    });
  }*/

  // Initialize checkout button
  const checkoutButton = document.getElementById('checkoutButton');
  if (checkoutButton) {
    checkoutButton.addEventListener('click', function (e) {
      e.preventDefault(); // Prevent the default link behavior
      console.log("Checkout button clicked");

      // Get the current cart items from Firebase and sync with server
      if (db && userId) {
        const cartRef = doc(db, 'carts', userId);
        getDoc(cartRef).then((docSnap) => {
          if (docSnap.exists()) {
            const cartData = docSnap.data();
            if (cartData.items && cartData.items.length > 0) {
              syncCartWithServer(cartData.items);
            } else {
              alert('Your cart is empty');
            }
          } else {
            alert('Your cart is empty');
          }
        });
      }
    });
  }

});
// Add this function to synchronize Firebase cart with server-side cart
function syncCartWithServer(cartItems) {
  fetch('/api/cart/sync', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(cartItems),
    credentials: 'same-origin'
  })
    .then(response => response.json())
    .then(data => {
      console.log('Cart synced with server:', data);
      // Now that the cart is synced, we can redirect to checkout
      window.location.href = '/Shop/Checkout';
    })
    .catch(error => {
      console.error('Error syncing cart:', error);
    });
}
