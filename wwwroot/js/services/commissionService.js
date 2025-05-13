/**
 * Commission Service
 * Handles commission-related operations for affiliates
 */
const commissionService = {
  /**
   * Get commission summary for an affiliate
   * @param {string} affiliateId - The ID of the affiliate
   * @returns {Promise<Object>} - Summary object with total, paid, and unpaid amounts
   */
  getCommissionSummary: async function (affiliateId) {
    try {
      console.log('Getting commission summary for affiliate:', affiliateId);

      // Get all commissions for this affiliate
      const commissions = await this.getAllCommissions(affiliateId);

      // Calculate totals
      let total = 0;
      let paid = 0;
      let unpaid = 0;

      commissions.forEach(commission => {
        const amount = parseFloat(commission.Amount || 0);
        total += amount;

        if (commission.IsPaid) {
          paid += amount;
        } else {
          unpaid += amount;
        }
      });

      return {
        total: total.toFixed(2),
        paid: paid.toFixed(2),
        unpaid: unpaid.toFixed(2)
      };
    } catch (error) {
      console.error('Error getting commission summary:', error);
      return {
        total: '0.00',
        paid: '0.00',
        unpaid: '0.00'
      };
    }
  },

  /**
   * Get all commissions for an affiliate
   * @param {string} affiliateId - The ID of the affiliate
   * @returns {Promise<Array>} - Array of commission objects
   */
  getAllCommissions: async function (affiliateId) {
    try {
      console.log('Getting all commissions for affiliate:', affiliateId);

      // Query the commissions collection
      const snapshot = await firebase
        .firestore()
        .collection('commissions')
        .where('AffiliateId', '==', affiliateId)
        .get();

      // If no commissions found, try alternative field names
      if (snapshot.empty) {
        console.log('No commissions found with AffiliateId, trying ReferenceId');

        const altSnapshot = await firebase
          .firestore()
          .collection('commissions')
          .where('ReferenceId', '==', affiliateId)
          .get();

        if (altSnapshot.empty) {
          console.log('No commissions found with alternative field names either');
          return [];
        }

        return this.processCommissions(altSnapshot.docs);
      }

      return this.processCommissions(snapshot.docs);
    } catch (error) {
      console.error('Error getting commissions:', error);
      return [];
    }
  },

  /**
   * Process commission documents to create usable objects
   * @param {Array} docs - Firestore document snapshots
   * @returns {Array} - Processed commission objects
   */
  processCommissions: function (docs) {
    return docs.map(doc => {
      const data = doc.data();
      console.log('Processing commission document:', data);

      // Handle date formatting
      let formattedDate = 'Unknown';
      const dateField = data.CreatedAt || data.PaidDate;

      if (dateField) {
        const date = dateField.toDate ? dateField.toDate() : new Date(dateField);
        formattedDate = date.toLocaleDateString();
      }

      // Handle OrderId properly - ensure it's a string
      let orderId = 'N/A';
      if (data.OrderId) {
        // If OrderId is an object with an id property (a reference)
        if (typeof data.OrderId === 'object' && data.OrderId.id) {
          orderId = data.OrderId.id;
        }
        // If OrderId is an object but doesn't have an id property
        else if (typeof data.OrderId === 'object') {
          orderId = JSON.stringify(data.OrderId).substring(0, 10) + '...';
        }
        // If OrderId is already a string
        else {
          orderId = String(data.OrderId);
        }
      }

      // Handle ProductId properly as well
      let productId = 'N/A';
      if (data.ProductId) {
        if (typeof data.ProductId === 'object' && data.ProductId.id) {
          productId = data.ProductId.id;
        } else if (typeof data.ProductId === 'object') {
          productId = JSON.stringify(data.ProductId).substring(0, 10) + '...';
        } else {
          productId = String(data.ProductId);
        }
      }

      // Check the rate specifically
      let rate = data.Rate || 0;
      // Log warning if rate is suspiciously close to 4%
      if (Math.abs(parseFloat(rate) - 4) < 0.1) {
        console.warn(`Commission ${doc.id} has a suspicious 4% rate. Raw data:`, data);
      }

      // Enrich with order and product details if needed
      return {
        commissionId: doc.id,
        OrderId: orderId,
        ProductId: productId,
        Amount: data.Amount || 0,
        Rate: rate,
        IsPaid: data.IsPaid || false,
        formattedDate: formattedDate
      };
    });
  },

  /**
   * Mark a commission as paid
   * @param {string} commissionId - The ID of the commission to mark as paid
   * @returns {Promise<boolean>} - Success status
   */
  markAsPaid: async function (commissionId) {
    try {
      console.log('Marking commission as paid:', commissionId);

      await firebase.firestore().collection('commissions').doc(commissionId).update({
        IsPaid: true,
        PaidDate: firebase.firestore.FieldValue.serverTimestamp(),
        Status: 'Paid'
      });

      return true;
    } catch (error) {
      console.error('Error marking commission as paid:', error);
      return false;
    }
  },

  /**
   * Calculate commission for a product
   * @param {string} productId - The ID of the product
   * @param {number} quantity - Quantity of the product
   * @param {number} price - Price of the product
   * @returns {Promise<Object>} - Commission calculation result
   */
  calculateCommissionForProduct: async function (productId, quantity, price) {
    try {
      console.log(`Calculating commission for product ${productId}, quantity=${quantity}, price=${price}`);

      // Get product details
      const productDoc = await firebase.firestore().collection('products').doc(productId).get();

      if (!productDoc.exists) {
        console.error('Product not found:', productId);
        return { amount: 0, rate: 0 };
      }

      const product = productDoc.data();
      console.log('Product data for commission calculation:', product);

      let commissionRate = product.Commission;
      console.log(`Product commission rate: ${commissionRate}`);

      // If product doesn't have commission rate, check the brand
      if (!commissionRate && product.BrandId) {
        const brandDoc = await firebase.firestore().collection('brands').doc(product.BrandId).get();

        if (brandDoc.exists) {
          const brandData = brandDoc.data();
          commissionRate = brandData.CommissionRate;
          console.log(`Using brand commission rate: ${commissionRate} from brand ${product.BrandId}`, brandData);
        } else {
          console.log(`Brand ${product.BrandId} not found for commission rate`);
        }
      }

      // IMPORTANT: Make sure we're not using a default of 4%
      if (!commissionRate) {
        console.log('No commission rate found, using 0%');
        commissionRate = 0;
      }

      // Calculate commission
      const subtotal = quantity * price;
      const commissionAmount = subtotal * (commissionRate / 100);

      console.log(`Final commission calculation: ${quantity} × ${price} × ${commissionRate}% = ${commissionAmount}`);

      return {
        amount: commissionAmount,
        rate: commissionRate
      };
    } catch (error) {
      console.error('Error calculating commission:', error);
      return { amount: 0, rate: 0 };
    }
  },

  /**
   * Create commission record for an order
   * @param {Object} order - The order object
   * @param {string} affiliateId - The affiliate ID
   * @returns {Promise<boolean>} - Success status
   */
  createCommissionForOrder: async function (order, affiliateId) {
    try {
      // Get order details
      const orderDetails = await firebase.firestore().collection('orderDetails').where('OrderId', '==', order.id).get();

      if (orderDetails.empty) {
        console.error('No order details found for order:', order.id);
        return false;
      }

      // Process each product in the order
      for (const detail of orderDetails.docs) {
        const item = detail.data();

        const commission = await this.calculateCommissionForProduct(item.ProductId, item.Quantity, item.Price);

        // Create commission record
        if (commission.amount > 0) {
          await firebase.firestore().collection('commissions').add({
            OrderId: order.id,
            ProductId: item.ProductId,
            AffiliateId: affiliateId,
            CustomerId: order.UserId,
            Amount: commission.amount,
            Rate: commission.rate,
            IsPaid: false,
            Status: 'Pending',
            CreatedAt: firebase.firestore.FieldValue.serverTimestamp()
          });
        }
      }

      return true;
    } catch (error) {
      console.error('Error creating commission:', error);
      return false;
    }
  }
};
