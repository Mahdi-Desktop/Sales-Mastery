/**
 * Affiliate Customers
 *//*

'use strict';

// DataTable initialization
$(function () {
  let dt_customer_table = $('.datatable-customers');

  // DataTable with buttons
  if (dt_customer_table.length) {
    var dt_customer = dt_customer_table.DataTable({
      dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
      lengthMenu: [10, 25, 50, 75, 100],
      // Use pagingType for pagination type
      pagingType: 'full_numbers',
      // For responsive
      responsive: true,
      language: {
        search: '',
        searchPlaceholder: 'Search customers',
        lengthMenu: '_MENU_ records per page'
      },
      // Order settings
      order: [[2, 'desc']] // Default sort by joined date, newest first
    });
  }

  // Filter form control to default size
  $('.dataTables_filter .form-control').removeClass('form-control-sm');
  $('.dataTables_length .form-select').removeClass('form-select-sm');
});
*/
/**
 * Affiliate Customers
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // Wait for Firebase to be ready
  if (window.db) {
    initializeCustomersTable(window.db);
  } else {
    document.addEventListener('firebase-ready', function (e) {
      initializeCustomersTable(e.detail.db);
    });
  }
});

function initializeCustomersTable(db) {
  // Get current user ID from user context
  const currentUserId = getCurrentUserId();

  if (!currentUserId) {
    console.error('User ID not found');
    showError('Authentication error', 'Please log in to view customer data');
    return;
  }

  // Get affiliate ID (current user ID if affiliate, or selected affiliate ID if admin)
  const affiliateId = isUserAdmin() && window.selectedAffiliateId ?
    window.selectedAffiliateId : currentUserId;

  // DOM Elements
  const customersTable = document.querySelector('.datatable-customers');
  const searchInput = document.getElementById('searchCustomers');

  // Show loading state
  if (customersTable) {
    customersTable.innerHTML = `
      <div class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading customers...</span>
        </div>
        <p class="mt-2">Loading customer data...</p>
      </div>
    `;
  }

  // Load customers data
  loadCustomersData(db, affiliateId)
    .then(customers => {
      renderCustomersTable(customers);

      // Initialize DataTable
      let dt_customer = $('.datatable-customers').DataTable({
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
        lengthMenu: [10, 25, 50, 75, 100],
        pagingType: 'full_numbers',
        responsive: true,
        language: {
          search: '',
          searchPlaceholder: 'Search customers',
          lengthMenu: '_MENU_ records per page'
        },
        order: [[2, 'desc']] // Default sort by joined date, newest first
      });

      // Apply custom search
      if (searchInput) {
        searchInput.addEventListener('keyup', function () {
          dt_customer.search(this.value).draw();
        });
      }

      // Filter form control to default size
      $('.dataTables_filter .form-control').removeClass('form-control-sm');
      $('.dataTables_length .form-select').removeClass('form-select-sm');
    })
    .catch(error => {
      console.error('Error loading customers:', error);
      showError('Data Loading Error', 'Failed to load customer data. Please try again.');
    });

  // Load customers data from Firestore
  async function loadCustomersData(db, affiliateId) {
    try {
      // Query customers referred by this affiliate
      const customersSnapshot = await db.collection('users')
        .where('Role', '==', 3) // Role 3 is customer
        .where('AffiliateId', '==', affiliateId)
        .get();

      if (customersSnapshot.empty) {
        return [];
      }

      const customers = [];

      // Process each customer
      for (const doc of customersSnapshot.docs) {
        const customer = doc.data();
        customer.id = doc.id;

        // Get customer's orders
        const ordersSnapshot = await db.collection('orders')
          .where('CustomerId', '==', doc.id)
          .get();

        let totalOrders = 0;
        let totalSpent = 0;
        let totalCommission = 0;

        ordersSnapshot.forEach(orderDoc => {
          const order = orderDoc.data();
          totalOrders++;
          totalSpent += parseFloat(order.Total || 0);
          totalCommission += parseFloat(order.Commission || 0);
        });

        // Add calculated fields
        customer.totalOrders = totalOrders;
        customer.totalSpent = totalSpent;
        customer.totalCommission = totalCommission;
        customer.joinedDate = customer.CreatedAt ? customer.CreatedAt.toDate() : new Date();

        customers.push(customer);
      }

      return customers;
    } catch (error) {
      console.error('Error loading customers data:', error);
      throw error;
    }
  }

  // Render customers table
  function renderCustomersTable(customers) {
    if (!customersTable) return;

    if (customers.length === 0) {
      customersTable.innerHTML = `
        <div class="text-center py-5">
          <i class="ti ti-users text-primary" style="font-size: 3rem;"></i>
          <h3 class="mt-3">No Customers Found</h3>
          <p class="mb-0">You don't have any referred customers yet.</p>
          <p class="mt-3">Share your affiliate link to start earning commissions.</p>
        </div>
      `;
      return;
    }

    // Create table HTML
    let tableHtml = `
      <table class="table datatable-customers">
        <thead>
          <tr>
            <th>Customer</th>
            <th>Email</th>
            <th>Joined Date</th>
            <th>Orders</th>
            <th>Amount Spent</th>
            <th>Commissions Earned</th>
          </tr>
        </thead>
        <tbody>
    `;

    // Add customer rows
    customers.forEach(customer => {
      const initials = getInitials(customer.Name || customer.Email || 'Customer');
      const formattedDate = formatDate(customer.joinedDate);

      tableHtml += `
                <tr>
          <td>
            <div class="d-flex align-items-center">
              <div class="avatar avatar-sm me-3">
                <div class="avatar-initial bg-label-primary rounded-circle">${initials}</div>
              </div>
              <div>
                <span class="fw-medium">${customer.Name || 'Customer'}</span>
              </div>
            </div>
          </td>
          <td>${customer.Email || '-'}</td>
          <td>${formattedDate}</td>
          <td>${customer.totalOrders}</td>
          <td>$${customer.totalSpent.toFixed(2)}</td>
          <td>$${customer.totalCommission.toFixed(2)}</td>
        </tr>
      `;
    });

    tableHtml += `
        </tbody>
      </table>
    `;

    customersTable.innerHTML = tableHtml;
  }

  // Show error message
  function showError(title, message) {
    if (!customersTable) return;

    customersTable.innerHTML = `
      <div class="alert alert-danger d-flex align-items-center" role="alert">
        <i class="ti ti-alert-circle me-2"></i>
        <div>
          <h6 class="alert-heading mb-1">${title}</h6>
          <span>${message}</span>
        </div>
      </div>
    `;
  }

  // Helper function to get initials from name
  function getInitials(name) {
    if (!name) return 'NA';

    const parts = name.split(' ');
    if (parts.length === 1) {
      return name.charAt(0).toUpperCase();
    }

    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  // Helper function to format date
  function formatDate(date) {
    if (!date) return '-';

    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }
}

