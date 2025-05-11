document.addEventListener('DOMContentLoaded', function () {
  // Initialize Firestore
  if (typeof firebase === 'undefined' || !firebase.apps.length) {
    console.log("Waiting for Firebase to initialize...");
    document.addEventListener('firebase-ready', function () {
      initializeCheckout(firebase.firestore());
    });
  } else {
    initializeCheckout(firebase.firestore());
  }

  function initializeCheckout(db) {
    // Get user ID
    const userId = getStoredUserId();
    console.log("Checkout.js - User ID:", userId);

    if (!userId) {
      window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname);
      return;
    }

    // DOM Elements
    const checkoutForm = document.getElementById('checkoutForm');
    const cartItemsContainer = document.getElementById('checkoutItems');
    const subtotalElement = document.getElementById('subtotal');
    const totalElement = document.getElementById('total');
    const submitButton = document.getElementById('submitCheckout');

    // Load cart and user information
    loadCheckoutData();

    async function loadCheckoutData() {
      try {
        console.log("Loading checkout data...");
        // Load user data
        const userDoc = await db.collection('users').doc(userId).get();
        let userData = null;
        let referringAffiliateId = null;

        if (userDoc.exists) {
          userData = userDoc.data();
          console.log("User data loaded:", userData);

          // Check if user has a referring affiliate
          if (userData.AffiliateId) {
            referringAffiliateId = userData.AffiliateId;
            console.log("User has referring affiliate:", referringAffiliateId);
          }

          // Pre-fill user information if available
          if (userData.PhoneNumber) {
            document.getElementById('phone').value = userData.PhoneNumber;
          }

          // Check if user has default address
          const addressesSnapshot = await db.collection('addresses')
            .where('UserId', '==', userId)
            .limit(1)
            .get();

          if (!addressesSnapshot.empty) {
            const defaultAddress = addressesSnapshot.docs[0].data();
            document.getElementById('address').value = defaultAddress.Street || '';
            document.getElementById('city').value = defaultAddress.City || '';
            document.getElementById('state').value = defaultAddress.State || defaultAddress.Governorate || '';
            document.getElementById('zipCode').value = defaultAddress.ZipCode || '';
          }
        }

        // Load cart data
        const cartDoc = await db.collection('carts').doc(userId).get();
        if (!cartDoc.exists || !cartDoc.data().items || cartDoc.data().items.length === 0) {
          window.location.href = '/Shop/Cart';
          return;
        }

        const cart = cartDoc.data();
        console.log("Cart data loaded:", cart);

        // Get up-to-date product information for each item
        const cartItemPromises = cart.items.map(item => {
          // Handle both direct productId and reference format
          let productId = item.productId;
          if (typeof productId === 'string' && productId.includes('/')) {
            productId = productId.split('/')[1];
          }

          return db.collection('products').doc(productId).get()
            .then(productDoc => {
              if (!productDoc.exists) {
                return null; // Product no longer exists
              }

              const product = productDoc.data();
              return {
                ...item,
                currentPrice: product.Price,
                currentDiscount: product.Discount || 0,
                stock: product.Stock,
                // Make sure we have the product info even if the cart item is outdated
                name: item.name || product.Name,
                imageUrl: item.imageUrl || (product.Image && product.Image.length > 0 ? product.Image[0] : null),
                brandId: item.brandId || product.BrandId,
                productId: productId // Clean productId
              };
            })
            .catch(error => {
              console.error('Error fetching product:', productId, error);
              return null;
            });
        });

        const cartItems = await Promise.all(cartItemPromises);
        console.log("Cart items with product details:", cartItems);

        // Filter out any null items (products that no longer exist)
        const validCartItems = cartItems.filter(item => item !== null);

        if (validCartItems.length === 0) {
          window.location.href = '/Shop/Cart';
          return;
        }

        // Check for stock issues
        const stockIssues = validCartItems.filter(item => item.stock < item.quantity);
        if (stockIssues.length > 0) {
          const issueMessages = stockIssues.map(item =>
            `${item.name}: requested ${item.quantity}, only ${item.stock} available`
          );

          alert('Some items in your cart have stock issues. Please update your cart before checkout.\n' +
            issueMessages.join('\n'));

          setTimeout(() => {
            window.location.href = '/Shop/Cart';
          }, 3000);
          return;
        }

        renderCheckoutItems(validCartItems);

        // Store cart data for submission with affiliate info
        sessionStorage.setItem('checkoutCartItems', JSON.stringify(validCartItems));
        sessionStorage.setItem('referringAffiliateId', referringAffiliateId || '');
      } catch (error) {
        console.error("Error loading checkout data: ", error);
        alert('Error loading checkout data. Please try again later.');
      }
    }

    // Render cart items in checkout summary
    function renderCheckoutItems(cartItems) {
      let html = '';
      let subtotal = 0;

      cartItems.forEach(item => {
        const effectivePrice = item.currentDiscount > 0
          ? item.currentPrice - (item.currentPrice * item.currentDiscount / 100)
          : item.currentPrice;

        const itemSubtotal = effectivePrice * item.quantity;
        subtotal += itemSubtotal;

        html += `
          <tr>
            <td class="text-nowrap">
              <div class="d-flex align-items-center">
                <div class="avatar avatar-sm me-2">
                  <img src="${item.imageUrl || '/img/products/default.jpg'}"
                       alt="${item.name}" class="rounded">
                </div>
                <div>${item.name} <span class="text-muted">× ${item.quantity}</span></div>
              </div>
            </td>
            <td class="text-end">${itemSubtotal.toFixed(2)}</td>
          </tr>
        `;
      });

      if (cartItemsContainer) {
        cartItemsContainer.innerHTML = html;
      }

      // Update totals
      const shipping = 4; // $4 shipping fee
      const total = subtotal + shipping;

      if (subtotalElement) subtotalElement.textContent = subtotal.toFixed(2);
      if (totalElement) totalElement.textContent = total.toFixed(2);
    }

    // Process checkout form submission
    if (checkoutForm) {
      checkoutForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        console.log("Checkout form submitted");

        // Disable submit button to prevent double submission
        if (submitButton) {
          submitButton.disabled = true;
          submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
        }

        try {
          // Get form data
          const address = document.getElementById('address').value;
          const city = document.getElementById('city').value;
          const state = document.getElementById('state').value;
          const zipCode = document.getElementById('zipCode').value;
          const phone = document.getElementById('phone').value;
          const paymentMethod = document.querySelector('input[name="paymentMethod"]:checked').value;

          console.log("Form data collected:", { address, city, state, zipCode, phone, paymentMethod });

          // Get cart items
          const cartDoc = await db.collection('carts').doc(userId).get();

          if (!cartDoc.exists || !cartDoc.data().items || cartDoc.data().items.length === 0) {
            alert('Your cart is empty');
            if (submitButton) {
              submitButton.disabled = false;
              submitButton.innerHTML = '<i class="ti ti-check me-1"></i> Place Order';
            }
            return;
          }

          const cart = cartDoc.data();
          const cartItems = cart.items;

          console.log("Cart items retrieved:", cartItems);

          // Calculate totals
          let subtotal = 0;
          cartItems.forEach(item => {
            const price = item.price || 0;
            const discount = item.discount || 0;
            const effectivePrice = discount > 0 ? price - (price * discount / 100) : price;
            subtotal += effectivePrice * item.quantity;
          });

          const shipping = 4; // $4 shipping fee
          const total = subtotal + shipping;

          console.log("Order totals calculated:", { subtotal, shipping, total });

          // Get user data to check if they're a customer with a referring affiliate
          const userDoc = await db.collection('users').doc(userId).get();
          let referringAffiliateId = null;
          let commissionDetails = [];
          let totalCommission = 0;

          if (userDoc.exists) {
            const userData = userDoc.data();

            // Check if user has a referring affiliate
            if (userData.AffiliateId) {
              referringAffiliateId = userData.AffiliateId;
              console.log("User has referring affiliate:", referringAffiliateId);

              // Calculate commissions for each product
              for (const item of cartItems) {
                if (item.brandId) {
                  try {
                    let brandId = item.brandId;
                    if (typeof brandId === 'string' && brandId.includes('/')) {
                      brandId = brandId.split('/')[1];
                    }

                    const brandDoc = await db.collection('brands').doc(brandId).get();

                    if (brandDoc.exists) {
                      const brand = brandDoc.data();
                      const commissionRate = brand.CommissionRate || 0;

                      if (commissionRate > 0) {
                        const price = item.price || 0;
                        const discount = item.discount || 0;
                        const effectivePrice = discount > 0 ? price - (price * discount / 100) : price;
                        const itemTotal = effectivePrice * item.quantity;

                        const itemCommission = itemTotal * (commissionRate / 100);
                        totalCommission += itemCommission;

                        commissionDetails.push({
                          productId: item.productId,
                          productName: item.name,
                          quantity: item.quantity,
                          price: price,
                          discount: discount,
                          subtotal: itemTotal,
                          brandId: item.brandId,
                          commissionRate: commissionRate,
                          commissionAmount: itemCommission
                        });
                      }
                    }
                  } catch (error) {
                    console.error(`Error calculating commission for product ${item.productId}:`, error);
                  }
                }
              }
            }
          }

          // Create order
          const orderData = {
            UserId: userId,
            Status: 'Pending',
            TotalAmount: total,
            OrderDate: firebase.firestore.FieldValue.serverTimestamp(),
            PaymentMethod: paymentMethod,
            ShippingAddress: {
              Address: address,
              City: city,
              State: state,
              ZipCode: zipCode
            },
            ContactPhone: phone,
            Items: cartItems,
            Subtotal: subtotal,
            ShippingFee: shipping,
            CreatedAt: firebase.firestore.FieldValue.serverTimestamp(),
            UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
          };

          // Add affiliate information if applicable
          if (referringAffiliateId) {
            orderData.AffiliateId = referringAffiliateId;
            orderData.Commission = {
              AffiliateId: referringAffiliateId,
              Details: commissionDetails,
              TotalCommission: totalCommission,
              Status: 'Pending'
            };
          }

          console.log("Creating order with data:", orderData);

          // Create the order
          const orderRef = await db.collection('orders').add(orderData);
          console.log("Order created with ID:", orderRef.id);

          // Update the user document to add this order ID to their OrderId array
          try {
            // Get the current user document
            const userDoc = await db.collection('users').doc(userId).get();

            if (userDoc.exists) {
              // Get the current OrderId array or create an empty one if it doesn't exist
              const userData = userDoc.data();
              const currentOrderIds = userData.OrderId || [];

              // Add the new order ID to the array
              await db.collection('users').doc(userId).update({
                OrderId: firebase.firestore.FieldValue.arrayUnion(orderRef.id),
                UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
              });

              console.log("User document updated with new order ID");
            }
          } catch (error) {
            console.error("Error updating user with order ID:", error);
            // Continue with the checkout process even if this update fails
          }

          // Save the shipping address for future use
          try {
            // Check if this address already exists for the user
            const addressesSnapshot = await db.collection('addresses')
              .where('UserId', '==', userId)
              .where('Street', '==', address)
              .where('City', '==', city)
              .limit(1)
              .get();

            if (addressesSnapshot.empty) {
              // Address doesn't exist, create a new one
              await db.collection('addresses').add({
                UserId: userId,
                Street: address,
                City: city,
                State: state,
                ZipCode: zipCode,
                IsDefault: true, // Mark as default address
                CreatedAt: firebase.firestore.FieldValue.serverTimestamp(),
                UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
              });

              console.log("New address saved for user");
            }
          } catch (error) {
            console.error("Error saving address:", error);
            // Continue with the checkout process even if this update fails
          }

          // Create detailed order items in the orderDetails collection
          try {
            const orderItemPromises = cartItems.map(item => {
              // Handle both direct productId and reference format
              let productId = item.productId;
              if (typeof productId === 'string' && productId.includes('/')) {
                productId = productId.split('/')[1];
              }

              // Create a reference to the product
              const productRef = db.collection('products').doc(productId);

              // Create a reference to the order
              const orderRef = db.collection('orders').doc(orderRef.id);

              // Create the order detail document
              return db.collection('orderDetails').add({
                OrderId: orderRef,
                ProductId: productRef,
                ProductName: item.name,
                Quantity: item.quantity,
                Price: item.price || item.currentPrice,
                SubTotal: (item.price || item.currentPrice) * item.quantity,
                SKU: item.sku || '',
                CreatedAt: firebase.firestore.FieldValue.serverTimestamp()
              });
            });

            await Promise.all(orderItemPromises);
            console.log("Order details created for all items");
          } catch (error) {
            console.error("Error creating order details:", error);
            // Continue with the checkout process even if this update fails
          }

          // Generate invoice
          const invoiceData = {
            OrderId: orderRef.id,
            UserId: userId,
            InvoiceNumber: generateInvoiceNumber(),
            InvoiceDate: firebase.firestore.FieldValue.serverTimestamp(),
            DueDate: firebase.firestore.Timestamp.fromDate(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)), // Due in 7 days
            Items: cartItems,
            Subtotal: subtotal,
            ShippingFee: shipping,
            TotalAmount: total,
            Status: 'Pending',
            PaymentMethod: paymentMethod,
            Notes: 'Thank you for your business!',
            CreatedAt: firebase.firestore.FieldValue.serverTimestamp(),
            UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
          };

          // Add affiliate commission to invoice if applicable
          if (referringAffiliateId && totalCommission > 0) {
            invoiceData.Commission = {
              AffiliateId: referringAffiliateId,
              Amount: totalCommission,
              Status: 'Pending'
            };
          }

          console.log("Creating invoice with data:", invoiceData);

          const invoiceRef = await db.collection('invoices').add(invoiceData);
          console.log("Invoice created with ID:", invoiceRef.id);

          // Update the order with invoice reference
          await orderRef.update({
            InvoiceId: invoiceRef.id
          });

          // Update the user document to add this invoice ID to their InvoiceId array
          // Update the user document to add this order ID to their OrderId array
          try {
            // Create a reference to the order document
            const orderReference = db.collection('orders').doc(orderRef.id);

            await db.collection('users').doc(userId).update({
              OrderId: firebase.firestore.FieldValue.arrayUnion(orderReference),
              UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
            });
            console.log("User document updated with new order ID reference");
          } catch (error) {
            console.error("Error updating user with order ID:", error);
          }


          // If there's a referring affiliate, update their earnings record
          if (referringAffiliateId && totalCommission > 0) {
            // Create or update affiliate earnings
            const earningRef = db.collection('affiliateEarnings').doc();
            await earningRef.set({
              AffiliateId: referringAffiliateId,
              OrderId: orderRef.id,
              CustomerId: userId,
              Amount: totalCommission,
              Status: 'Pending',
              Details: commissionDetails,
              CreatedAt: firebase.firestore.FieldValue.serverTimestamp()
            });
            console.log("Affiliate earnings record created");
          }

          // Clear the cart
          await db.collection('carts').doc(userId).update({
            items: [],
            updatedAt: firebase.firestore.FieldValue.serverTimestamp()
          });

          console.log("Cart cleared");

          // Redirect to order confirmation
          sessionStorage.setItem('lastOrderId', orderRef.id);
          window.location.href = `/Shop/OrderConfirmation?orderId=${orderRef.id}`;

        } catch (error) {
          console.error("Error processing checkout:", error);
          alert('An error occurred during checkout. Please try again.');

          // Re-enable submit button
          if (submitButton) {
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="ti ti-check me-1"></i> Place Order';
          }
        }
      });
    }

    // Generate invoice number
    function generateInvoiceNumber() {
      const date = new Date();
      const year = date.getFullYear().toString().slice(-2);  // Using slice() instead of substr()
      const month = (date.getMonth() + 1).toString().padStart(2, '0');
      const random = Math.floor(Math.random() * 10000).toString().padStart(4, '0');

      return `INV-${year}${month}-${random}`;
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
});
