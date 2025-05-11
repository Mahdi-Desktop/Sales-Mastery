document.addEventListener('DOMContentLoaded', function () {
  // Initialize Firestore
  const db = firebase.firestore();

  // Get invoice ID from URL
  const urlParams = new URLSearchParams(window.location.search);
  const invoiceId = urlParams.get('id');

  if (!invoiceId) {
    window.location.href = '/Shop/OrderHistory';
    return;
  }

  // Get user ID
  const userId = getStoredUserId();

  if (!userId) {
    window.location.href = '/Auth/LoginBasic?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
    return;
  }

  // DOM Elements
  const invoiceContainer = document.getElementById('invoice-content');
  const printBtn = document.getElementById('printBtn');
  const downloadPdfBtn = document.getElementById('downloadPdf');

  // Load invoice details
  function loadInvoiceDetails() {
    db.collection('invoices').doc(invoiceId).get()
      .then(async (doc) => {
        if (!doc.exists) {
          showError('Invoice not found');
          return;
        }

        const invoice = doc.data();
        invoice.id = doc.id;

        // Get order data
        let orderData = null;
        try {
          const orderDoc = await db.collection('orders').doc(invoice.orderId).get();
          if (orderDoc.exists) {
            orderData = orderDoc.data();
            orderData.id = orderDoc.id;

            // Verify this invoice belongs to the current user
            if (orderData.userId !== userId) {
              showError('You do not have permission to view this invoice');
              return;
            }
          }
        } catch (error) {
          console.error("Error loading order data:", error);
        }

        // Get user data
        let userData = null;
        let addressData = null;

        try {
          const userDoc = await db.collection('users').doc(userId).get();
          if (userDoc.exists) {
            userData = userDoc.data();

            // Get address data
            const addressesSnapshot = await db.collection('addresses')
              .where('userId', '==', userId)
              .limit(1)
              .get();

            if (!addressesSnapshot.empty) {
              addressData = addressesSnapshot.docs[0].data();
            }
          }
        } catch (error) {
          console.error("Error loading user data:", error);
        }

        renderInvoice(invoice, orderData, userData, addressData);
      })
      .catch((error) => {
        console.error("Error loading invoice: ", error);
        showError('An error occurred while loading invoice details');
      });
  }

  // Render invoice
  function renderInvoice(invoice, order, userData, addressData) {
    // Format dates
    const invoiceDate = invoice.invoiceDate.toDate();
    const dueDate = invoice.dueDate.toDate();

    const formattedInvoiceDate = invoiceDate.toLocaleDateString('en-US', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    });

    const formattedDueDate = dueDate.toLocaleDateString('en-US', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    });

    // Get customer name and contact info
    const customerName = userData ? `${userData.firstName} ${userData.lastName}` : 'Customer';
    const customerEmail = userData ? userData.email : '';
    const customerPhone = order?.contactPhone || userData?.phone || '';

    // Get shipping address
    let shippingAddress = '';
    if (order?.shippingAddress) {
      shippingAddress = `
                <div>${order.shippingAddress.street}</div>
                <div>${order.shippingAddress.city}, ${order.shippingAddress.governorate}</div>
                <div>${order.shippingAddress.zipCode}</div>
            `;
    } else if (addressData) {
      shippingAddress = `
                <div>${addressData.street}</div>
                <div>${addressData.city}, ${addressData.governorate}</div>
                <div>${addressData.zipCode}</div>
            `;
    }

    // Status badge
    const statusClass = invoice.status === 'Paid' ? 'success' :
      invoice.status === 'Pending' ? 'warning' : 'danger';

    // Render invoice content
    const html = `
            <div class="row mb-4">
                <div class="col-sm-6 mb-4 mb-sm-0">
                    <h6 class="mb-3">From:</h6>
                    <div class="mb-1">
                        <span class="fw-bold">Sales Mastery</span>
                    </div>
                    <div>123 Business Street</div>
                    <div>Beirut, Lebanon</div>
                    <div>Email: sales@salesmastery.com</div>
                    <div>Phone: +961 1 123 456</div>
                </div>
                <div class="col-sm-6">
                    <h6 class="mb-3">Bill To:</h6>
                    <div class="mb-1">
                        <span class="fw-bold">${customerName}</span>
                    </div>
                    ${shippingAddress}
                    <div>Email: ${customerEmail}</div>
                    <div>Phone: ${customerPhone}</div>
                </div>
            </div>

            <div class="row mb-4">
                <div class="col-sm-6 mb-4 mb-sm-0">
                    <h6 class="mb-3">Invoice Details:</h6>
                    <div><strong>Invoice Number:</strong> ${invoice.invoiceNumber}</div>
                    <div><strong>Invoice Date:</strong> ${formattedInvoiceDate}</div>
                    <div><strong>Due Date:</strong> ${formattedDueDate}</div>
                    <div><strong>Status:</strong> <span class="badge bg-${statusClass}">${invoice.status}</span></div>
                </div>
                <div class="col-sm-6">
                    <h6 class="mb-3">Payment Details:</h6>
                    <div><strong>Payment Method:</strong> ${invoice.paymentMethod || 'Cash on Delivery'}</div>
                </div>
            </div>

            <div class="table-responsive mb-4">
                <table class="table table-bordered">
                    <thead>
                        <tr>
                            <th>Item</th>
                            <th class="text-center">Quantity</th>
                            <th class="text-end">Unit Price</th>
                            <th class="text-end">Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${invoice.items.map(item => `
                            <tr>
                                <td>${item.name}</td>
                                <td class="text-center">${item.quantity}</td>
                                <td class="text-end">$${item.price.toFixed(2)}</td>
                                <td class="text-end">$${item.subtotal.toFixed(2)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                    <tfoot>
                        <tr>
                            <td colspan="3" class="text-end"><strong>Subtotal</strong></td>
                            <td class="text-end">$${invoice.subtotal.toFixed(2)}</td>
                        </tr>
                        <tr>
                            <td colspan="3" class="text-end"><strong>Delivery Fee</strong></td>
                            <td class="text-end">$${invoice.shippingFee.toFixed(2)}</td>
                        </tr>
                        <tr>
                            <td colspan="3" class="text-end"><strong>Total</strong></td>
                            <td class="text-end">$${invoice.totalAmount.toFixed(2)}</td>
                        </tr>
                    </tfoot>
                </table>
            </div>

            ${invoice.notes ? `
                <div class="mb-4">
                    <h6>Notes:</h6>
                    <p>${invoice.notes}</p>
                </div>
            ` : ''}

            <div class="row">
                <div class="col-12">
                    <div class="alert alert-info mb-0">
                        <h6>Thank you for your business!</h6>
                        <p class="mb-0">For any questions regarding this invoice, please contact our customer support at support@salesmastery.com</p>
                    </div>
                </div>
            </div>
        `;

    invoiceContainer.innerHTML = html;
  }

  // Show error message
  function showError(message) {
    invoiceContainer.innerHTML = `
            <div class="text-center py-5">
                <i class="ti ti-alert-triangle text-danger" style="font-size: 3rem;"></i>
                <h3 class="mt-3">Error</h3>
                <p class="mb-4">${message}</p>
                <a href="/Shop/OrderHistory" class="btn btn-primary">Go to Order History</a>
            </div>
        `;

    // Hide action buttons
    if (printBtn) printBtn.style.display = 'none';
    if (downloadPdfBtn) downloadPdfBtn.style.display = 'none';
  }

  // Set up print functionality
  if (printBtn) {
    printBtn.addEventListener('click', function () {
      window.print();
    });
  }

  // Set up PDF download functionality
  if (downloadPdfBtn) {
    downloadPdfBtn.addEventListener('click', function () {
      // You can implement PDF generation here using a library like jsPDF
      // For now, we'll use print as a fallback
      window.print();
    });
  }

  // Get user ID from storage
  function getStoredUserId() {
    return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
  }

  // Initialize
  loadInvoiceDetails();
});
