document.addEventListener('DOMContentLoaded', function () {
  // Initialize Firestore
  const db = firebase.firestore();

  // Get user ID
  const userId = getStoredUserId();

  if (!userId) {
    window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname);
    return;
  }

  // DOM Elements
  const ordersContainer = document.getElementById('ordersContainer');

  // Load order history
  function loadOrderHistory() {
    db.collection('orders')
      .where('UserId', '==', `users/${userId}`)
      .orderBy('OrderDate', 'desc')
      .get()
      .then((snapshot) => {
        if (snapshot.empty) {
          showEmptyOrderHistory();
          return;
        }

        const orders = [];
        snapshot.forEach(doc => {
          const order = doc.data();
          order.id = doc.id;
          orders.push(order);
        });

        renderOrderHistory(orders);
      })
      .catch((error) => {
        console.error("Error loading order history: ", error);
        ordersContainer.innerHTML = `
          <tr>
            <td colspan="5" class="text-center py-4">
              <i class="ti ti-alert-triangle text-danger mb-2" style="font-size: 2rem;"></i>
              <p>An error occurred while loading your order history. Please try again later.</p>
            </td>
          </tr>
        `;
      });
  }

  // Render order history
  function renderOrderHistory(orders) {
    let html = '';

    orders.forEach(order => {
      const orderDate = order.OrderDate.toDate();
      const formattedDate = orderDate.toLocaleDateString('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });

      // Determine badge class based on status
      let badgeClass = 'bg-secondary';
      switch (order.Status) {
        case 'Pending':
          badgeClass = 'bg-warning';
          break;
        case 'Processing':
          badgeClass = 'bg-info';
          break;
        case 'Shipped':
          badgeClass = 'bg-primary';
          break;
        case 'Delivered':
          badgeClass = 'bg-success';
          break;
        case 'Cancelled':
          badgeClass = 'bg-danger';
          break;
      }

      html += `
        <tr>
          <td>${order.id.substring(0, 8)}...</td>
          <td>${formattedDate}</td>
          <td>
            <span class="badge ${badgeClass}">${order.Status}</span>
          </td>
          <td>$${order.TotalAmount.toFixed(2)}</td>
          <td>
            <a href="/Shop/OrderConfirmation?orderId=${order.id}" class="btn btn-sm btn-outline-primary">
              <i class="ti ti-eye"></i>
            </a>
          </td>
        </tr>
      `;
    });

    ordersContainer.innerHTML = html;
  }

  // Show empty order history message
  function showEmptyOrderHistory() {
    document.querySelector('.table-responsive').style.display = 'none';

    const emptyMessageHtml = `
      <div class="text-center py-5">
        <i class="ti ti-shopping-bag text-primary" style="font-size: 3rem;"></i>
        <h3 class="mt-3">No orders found</h3>
        <p class="mb-4">You haven't placed any orders yet.</p>
        <a href="/Shop/Index" class="btn btn-primary">Start Shopping</a>
      </div>
    `;

    const container = document.createElement('div');
    container.innerHTML = emptyMessageHtml;
    ordersContainer.closest('.card-body').appendChild(container);
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

  // Initialize
  loadOrderHistory();
});



/*document.addEventListener('DOMContentLoaded', function () {
  // Initialize Firestore
  const db = firebase.firestore();

  // Get user ID
  const userId = getStoredUserId();

  if (!userId) {
    window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname);
    return;
  }

  // DOM Elements
  const ordersContainer = document.getElementById('ordersContainer');

  // Load order history
  function loadOrderHistory() {
    db.collection('orders')
      .where('userId', '==', userId)
      .orderBy('orderDate', 'desc')
      .get()
      .then((snapshot) => {
        if (snapshot.empty) {
          showEmptyOrderHistory();
          return;
        }

        const orders = [];
        snapshot.forEach(doc => {
          const order = doc.data();
          order.id = doc.id;
          orders.push(order);
        });

        renderOrderHistory(orders);
      })
      .catch((error) => {
        console.error("Error loading order history: ", error);
        if (ordersContainer) {
          ordersContainer.innerHTML = `
            <tr>
              <td colspan="5" class="text-center py-4">
                <i class="ti ti-alert-triangle text-danger mb-2" style="font-size: 2rem;"></i>
                <p>An error occurred while loading your order history. Please try again later.</p>
              </td>
            </tr>
          `;
        }
      });
  }

  // Render order history
  function renderOrderHistory(orders) {
    if (!ordersContainer) return;

    let html = '';

    orders.forEach(order => {
      const orderDate = order.orderDate.toDate();
      const formattedDate = orderDate.toLocaleDateString('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });

      // Determine badge class based on status
      let badgeClass = 'bg-secondary';
      switch (order.status) {
        case 'Pending':
          badgeClass = 'bg-warning';
          break;
        case 'Processing':
          badgeClass = 'bg-info';
          break;
        case 'Shipped':
          badgeClass = 'bg-primary';
          break;
        case 'Delivered':
          badgeClass = 'bg-success';
          break;
        case 'Cancelled':
          badgeClass = 'bg-danger';
          break;
      }

      html += `
        <tr>
          <td>${order.id.substring(0, 8)}...</td>
          <td>${formattedDate}</td>
          <td>
            <span class="badge ${badgeClass}">${order.status}</span>
          </td>
          <td>$${order.totalAmount.toFixed(2)}</td>
          <td>
            <a href="/Shop/OrderConfirmation?orderId=${order.id}" class="btn btn-sm btn-outline-primary">
              <i class="ti ti-eye"></i>
            </a>
          </td>
        </tr>
      `;
    });

    ordersContainer.innerHTML = html;
  }

  // Show empty order history message
  function showEmptyOrderHistory() {
    const tableResponsive = document.querySelector('.table-responsive');
    if (tableResponsive) {
      tableResponsive.style.display = 'none';
    }

    const emptyMessageHtml = `
      <div class="text-center py-5">
        <i class="ti ti-shopping-bag text-primary" style="font-size: 3rem;"></i>
        <h3 class="mt-3">No orders found</h3>
        <p class="mb-4">You haven't placed any orders yet.</p>
        <a href="/Shop/Index" class="btn btn-primary">Start Shopping</a>
      </div>
    `;

    const container = document.createElement('div');
    container.innerHTML = emptyMessageHtml;

    const cardBody = ordersContainer.closest('.card-body');
    if (cardBody) {
      cardBody.appendChild(container);
    }
  }

  // Get user ID from storage
  function getStoredUserId() {
    return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
  }

  // Initialize
  loadOrderHistory();
});
*/
