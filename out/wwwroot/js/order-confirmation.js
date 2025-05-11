document.addEventListener('DOMContentLoaded', function () {
  // Initialize Firestore
  if (typeof firebase === 'undefined' || !firebase.apps.length) {
    console.log("Waiting for Firebase to initialize...");
    document.addEventListener('firebase-ready', function () {
      initializeOrderConfirmation(firebase.firestore());
    });
  } else {
    initializeOrderConfirmation(firebase.firestore());
  }

  function initializeOrderConfirmation(db) {
    // Get user ID
    const userId = getStoredUserId();
    console.log("OrderConfirmation.js - User ID:", userId);

    if (!userId) {
      console.error("User ID not found");
      return;
    }

    // Get order ID from URL query parameter
    const urlParams = new URLSearchParams(window.location.search);
    const orderId = urlParams.get('orderId') || sessionStorage.getItem('lastOrderId');

    if (orderId) {
      // Display the order ID in the confirmation message
      const orderIdElement = document.getElementById('confirmedOrderId');
      if (orderIdElement) {
        orderIdElement.textContent = orderId;
      }

      // Store the order ID in session storage for reference
      sessionStorage.setItem('lastOrderId', orderId);
    }

    // Get the View My Orders button
    const viewOrdersButton = document.getElementById('viewOrdersButton');

    if (viewOrdersButton) {
      viewOrdersButton.addEventListener('click', function (e) {
        e.preventDefault();

        // Load and display order history
        loadOrderHistory(userId, db);

        // Show the order history section
        const orderHistorySection = document.getElementById('orderHistorySection');
        if (orderHistorySection) {
          orderHistorySection.style.display = 'block';
          orderHistorySection.scrollIntoView({ behavior: 'smooth' });
        }
      });
    }
  }

  async function loadOrderHistory(userId, db) {
    try {
      const orderHistoryElement = document.getElementById('orderHistory');
      if (!orderHistoryElement) {
        console.error("Order history element not found");
        return;
      }

      // Show loading indicator
      orderHistoryElement.innerHTML = `
                <div class="text-center py-3">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading order history...</span>
                    </div>
                </div>
            `;

      // Get the user document to get their order IDs
      const userDoc = await db.collection('users').doc(userId).get();

      if (!userDoc.exists) {
        console.error("User document not found");
        orderHistoryElement.innerHTML = `
                    <div class="alert alert-danger">
                        User information not found. Please contact support.
                    </div>
                `;
        return;
      }

      const userData = userDoc.data();
      const orderIds = userData.OrderId || [];

      if (orderIds.length === 0) {
        // Try to get orders directly by querying the orders collection
        console.log("No order IDs found in user document, querying orders collection directly");

        // Get orders for the user
        const ordersSnapshot = await db.collection('orders')
          .where('UserId', '==', userId)
          .get();

        if (ordersSnapshot.empty) {
          orderHistoryElement.innerHTML = `
                        <div class="alert alert-info">
                            No previous orders found.
                        </div>
                    `;
          return;
        }

        // Display orders
        displayOrders(ordersSnapshot, orderHistoryElement);
        return;
      }

      // Get the order documents using the order IDs
      const orderPromises = orderIds.map(orderId =>
        db.collection('orders').doc(orderId).get()
      );

      const orderDocs = await Promise.all(orderPromises);
      const validOrderDocs = orderDocs.filter(doc => doc.exists);

      if (validOrderDocs.length === 0) {
        orderHistoryElement.innerHTML = `
                    <div class="alert alert-info">
                        No previous orders found.
                    </div>
                `;
        return;
      }

      // Display orders
      displayOrdersFromDocs(validOrderDocs, orderHistoryElement);
    } catch (error) {
      console.error("Error loading order history:", error);
      const orderHistoryElement = document.getElementById('orderHistory');
      if (orderHistoryElement) {
        orderHistoryElement.innerHTML = `
                    <div class="alert alert-danger">
                        Error loading order history. Please try again later.
                    </div>
                `;
      }
    }
  }

  function displayOrders(ordersSnapshot, orderHistoryElement) {
    // Display orders
    let html = `
            <div class="table-responsive">
                <table class="table table-bordered">
                    <thead>
                        <tr>
                            <th>Order ID</th>
                            <th>Date</th>
                            <th>Total</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

    ordersSnapshot.forEach(doc => {
      const order = doc.data();
      const orderId = doc.id;
      const orderDate = order.OrderDate ? new Date(order.OrderDate.seconds * 1000) : new Date();
      const formattedDate = orderDate.toLocaleDateString();

      html += `
                <tr>
                    <td>${orderId}</td>
                    <td>${formattedDate}</td>
                    <td>$${order.TotalAmount.toFixed(2)}</td>
                    <td>
                        <span class="badge bg-${getStatusBadgeColor(order.Status)}">${order.Status}</span>
                    </td>
                </tr>
            `;
    });

    html += `
                    </tbody>
                </table>
            </div>
        `;

    orderHistoryElement.innerHTML = html;
  }

  function displayOrdersFromDocs(orderDocs, orderHistoryElement) {
    // Display orders
    let html = `
            <div class="table-responsive">
                <table class="table table-bordered">
                    <thead>
                        <tr>
                            <th>Order ID</th>
                            <th>Date</th>
                            <th>Total</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

    orderDocs.forEach(doc => {
      const order = doc.data();
      const orderId = doc.id;
      const orderDate = order.OrderDate ? new Date(order.OrderDate.seconds * 1000) : new Date();
      const formattedDate = orderDate.toLocaleDateString();

      html += `
                <tr>
                    <td>${orderId}</td>
                    <td>${formattedDate}</td>
                    <td>$${order.TotalAmount.toFixed(2)}</td>
                    <td>
                        <span class="badge bg-${getStatusBadgeColor(order.Status)}">${order.Status}</span>
                    </td>
                </tr>
            `;
    });

    html += `
                    </tbody>
                </table>
            </div>
        `;

    orderHistoryElement.innerHTML = html;
  }

  function getStatusBadgeColor(status) {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'warning';
      case 'processing':
        return 'info';
      case 'shipped':
        return 'primary';
      case 'delivered':
        return 'success';
      case 'cancelled':
        return 'danger';
      default:
        return 'secondary';
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
