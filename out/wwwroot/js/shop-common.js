// Add to cart function - reusable across pages
function addToCart(productId, quantity = 1) {
  console.log("Adding to cart:", productId, "quantity:", quantity); // Debug log

  const userId = getStoredUserId();
  console.log("User ID for cart:", userId); // Debug log

  if (!userId) {
    // Redirect to login if not logged in
    window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname);
    return;
  }

  // Check if Firebase is initialized
  if (typeof firebase === 'undefined' || !firebase.apps.length) {
    console.error("Firebase is not initialized yet");
    alert("Please wait a moment and try again. The system is still initializing.");
    return;
  }

  const db = firebase.firestore();
  const cartRef = db.collection('carts').doc(userId);

  // Get the product data
  db.collection('products')
    .doc(productId)
    .get()
    .then(doc => {
      console.log("Product exists:", doc.exists); // Debug log

      if (!doc.exists) {
        alert('Product not found');
        return;
      }

      const product = doc.data();
      console.log("Product data:", product); // Debug log

      // Check if the product is in stock
      if (product.Stock <= 0) {
        alert('This product is out of stock');
        return;
      }

      // Update the cart
      return cartRef.get().then(cartDoc => {
        console.log("Cart exists:", cartDoc.exists); // Debug log

        let cart;

        if (!cartDoc.exists) {
          // Create new cart if it doesn't exist
          console.log("Creating new cart"); // Debug log
          cart = {
            items: [],
            userId: userId,
            updatedAt: firebase.firestore.FieldValue.serverTimestamp()
          };
        } else {
          cart = cartDoc.data();
          console.log("Existing cart:", cart); // Debug log
          if (!cart.items) cart.items = [];
        }

        // Check if the product is already in the cart
        const existingItemIndex = cart.items.findIndex(item => {
          const itemProductId = typeof item.productId === 'string' && item.productId.includes('/')
            ? item.productId.split('/')[1]
            : item.productId;
          return itemProductId === productId;
        });

        console.log("Existing item index:", existingItemIndex); // Debug log

        if (existingItemIndex !== -1) {
          // Increment quantity if already in cart
          cart.items[existingItemIndex].quantity += quantity;

          // Recalculate subtotal
          const price = cart.items[existingItemIndex].price;
          const discount = cart.items[existingItemIndex].discount || 0;
          const effectivePrice = discount > 0 ? price - (price * discount / 100) : price;
          cart.items[existingItemIndex].subtotal = effectivePrice * cart.items[existingItemIndex].quantity;

          console.log("Updated existing item:", cart.items[existingItemIndex]); // Debug log
        } else {
          // Add new item to cart
          const effectivePrice = product.Discount > 0
            ? product.Price - (product.Price * product.Discount / 100)
            : product.Price;

          const newItem = {
            productId: productId,
            quantity: quantity,
            price: product.Price,
            name: product.Name,
            imageUrl: product.Image && product.Image.length > 0 ? product.Image[0] : null,
            discount: product.Discount || 0,
            subtotal: effectivePrice * quantity,
            brandId: product.BrandId || null
          };

          cart.items.push(newItem);
          console.log("Added new item:", newItem); // Debug log
        }

        cart.updatedAt = firebase.firestore.FieldValue.serverTimestamp();
        console.log("Saving cart:", cart); // Debug log

        // Save cart to Firestore
        return cartRef.set(cart);
      });
    })
    .then(() => {
      alert('Product added to cart');
      updateCartCount();
    })
    .catch(error => {
      console.error('Error adding to cart: ', error);
      alert('An error occurred while adding to cart: ' + error.message);
    });
}

// Update cart count in the header
function updateCartCount() {
  const userId = getStoredUserId();
  if (!userId) return;

  // Check if Firebase is initialized
  if (typeof firebase === 'undefined' || !firebase.apps.length) {
    console.error("Firebase is not initialized yet for updateCartCount");
    return;
  }

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
      console.error('Error updating cart count: ', error);
    });
}

// Calculate subtotal for an item
function calculateSubtotal(price, discount, quantity) {
  const effectivePrice = discount > 0 ? price - (price * discount / 100) : price;
  return effectivePrice * quantity;
}

// Get user ID from storage
function getStoredUserId() {
  // Try to get from user-data script first
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

  // Fall back to session/local storage
  return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
}

// Parse user data from the embedded script
window.userData = null;
document.addEventListener('DOMContentLoaded', function () {
  try {
    const userDataScript = document.getElementById('user-data');
    if (userDataScript) {
      window.userData = JSON.parse(userDataScript.textContent);
    }
  } catch (e) {
    console.error('Error parsing user data:', e);
  }

  // Wait for Firebase to be initialized before updating cart count
  if (typeof firebase !== 'undefined' && firebase.apps.length > 0) {
    updateCartCount();
  } else {
    document.addEventListener('firebase-ready', function () {
      updateCartCount();
    });
  }
});
