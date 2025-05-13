/**
 * Calculate affiliate discount based on commission rate
 * @param {number} price - The original product price
 * @param {number} commissionRate - The commission rate percentage
 * @returns {object} Object containing discounted price and discount amount
 */
function calculateAffiliateDiscount(price, commissionRate) {
  if (!price || !commissionRate) {
    return {
      originalPrice: price || 0,
      discountedPrice: price || 0,
      discountAmount: 0,
      discountPercentage: 0
    };
  }

  const originalPrice = parseFloat(price);
  const rate = parseFloat(commissionRate);

  if (isNaN(originalPrice) || isNaN(rate)) {
    return {
      originalPrice: price || 0,
      discountedPrice: price || 0,
      discountAmount: 0,
      discountPercentage: 0
    };
  }

  const discountAmount = (originalPrice * rate) / 100;
  const discountedPrice = originalPrice - discountAmount;

  return {
    originalPrice: originalPrice,
    discountedPrice: parseFloat(discountedPrice.toFixed(2)),
    discountAmount: parseFloat(discountAmount.toFixed(2)),
    discountPercentage: rate
  };
}

/**
 * Process affiliate purchase - applies commission as discount for affiliate purchases
 * @param {object} db - Firestore database instance
 * @param {string} affiliateId - The affiliate ID
 * @param {array} orderItems - The order items
 * @returns {Promise<array>} Enhanced order items with affiliate discounts
 */
async function processAffiliatePurchase(db, affiliateId, orderItems) {
  try {
    if (!affiliateId || !orderItems || !orderItems.length) {
      return orderItems;
    }

    // Enhance each item with commission rate if not already present
    const enhancedItems = await Promise.all(
      orderItems.map(async item => {
        // Skip if already has commission rate
        if (item.commissionRate) {
          // Apply commission as discount
          const discountInfo = calculateAffiliateDiscount(item.price, item.commissionRate);
          return {
            ...item,
            originalPrice: discountInfo.originalPrice,
            price: discountInfo.discountedPrice,
            discountAmount: discountInfo.discountAmount,
            discountPercentage: discountInfo.discountPercentage
          };
        }

        // Get commission rate for the product
        const productDoc = await db.collection('products').doc(item.productId).get();
        if (productDoc.exists) {
          const productData = productDoc.data();
          const commissionRate = parseFloat(productData.commissionRate || 0);

          // Apply commission as discount
          const discountInfo = calculateAffiliateDiscount(item.price, commissionRate);
          return {
            ...item,
            commissionRate: commissionRate,
            originalPrice: discountInfo.originalPrice,
            price: discountInfo.discountedPrice,
            discountAmount: discountInfo.discountAmount,
            discountPercentage: discountInfo.discountPercentage
          };
        }

        return item;
      })
    );

    return enhancedItems;
  } catch (error) {
    console.error('Error processing affiliate purchase:', error);
    return orderItems;
  }
}

/**
 * Get commission rate for a product
 * @param {object} db - Firestore database instance
 * @param {string} productId - The product ID
 * @returns {Promise<number>} The commission rate percentage
 */
async function getProductCommissionRate(db, productId) {
  try {
    if (!productId) {
      console.warn('Product ID is empty');
      return 0;
    }

    console.log(`Getting commission rate for product: ${productId}`);
    const productDoc = await db.collection('products').doc(productId).get();

    if (!productDoc.exists) {
      console.warn(`Product ${productId} not found`);
      return 0;
    }

    const product = productDoc.data();

    // Check direct commission rate first
    if (product.Commission !== undefined) {
      const rate = parseFloat(product.Commission);
      console.log(`Using direct commission rate from product: ${rate}%`);
      return rate;
    }

    if (product.commission !== undefined) {
      const rate = parseFloat(product.commission);
      console.log(`Using direct commission rate from product (lowercase): ${rate}%`);
      return rate;
    }

    // Check brand commission rate
    const brandId = product.BrandId || product.brandId;
    if (brandId) {
      console.log(`Looking for brand: ${brandId}`);
      const brandDoc = await db.collection('brands').doc(brandId).get();

      if (brandDoc.exists) {
        const brandData = brandDoc.data();
        const rate = parseFloat(brandData.CommissionRate || 0);
        console.log(`Using brand commission rate: ${rate}%`);
        return rate;
      }
    }

    // Check brand name for hardcoded rates
    const brandName = product.Brand || product.brand;
    if (brandName) {
      console.log(`Checking brand name: ${brandName}`);
      const lowerBrand = brandName.toLowerCase();

      if (lowerBrand.includes('optimal')) {
        console.log('Brand is Optimal, using 10% rate');
        return 10;
      } else if (lowerBrand.includes('loris')) {
        console.log('Brand is Loris, using 30% rate');
        return 30;
      } else if (lowerBrand.includes('dermokil')) {
        console.log('Brand is Dermokil, using 25% rate');
        return 25;
      }
    }

    console.warn(`No commission rate found for product ${productId}`);
    return 0;
  } catch (error) {
    console.error(`Error getting commission rate for product ${productId}:`, error);
    return 0;
  }
}

/**
 * Calculate commission for a product
 * @param {number} price - The product price
 * @param {number} quantity - The quantity purchased
 * @param {number} commissionRate - The commission rate percentage
 * @returns {number} The commission amount
 */
function calculateProductCommission(price, quantity, commissionRate) {
  if (!price || !quantity || !commissionRate) {
    return 0;
  }

  const subtotal = parseFloat(price) * parseInt(quantity);
  return subtotal * (parseFloat(commissionRate) / 100);
}

/**
 * Create commission records from an order
 * @param {object} db - Firestore database instance
 * @param {string} orderId - The order ID
 * @param {string} customerId - The customer ID
 * @param {string} affiliateId - The affiliate ID
 * @param {array} items - The order items with commission rates
 * @returns {Promise<array>} The created commission records
 */
async function createCommissionsFromOrder(db, orderId, customerId, affiliateId, items) {
  try {
    console.log(`Creating commissions for order ${orderId}, affiliate ${affiliateId}`);

    if (!affiliateId || !items || !items.length) {
      console.warn('Missing affiliate ID or items');
      return [];
    }

    // Calculate commissions for each item
    const commissions = [];

    for (const item of items) {
      const commissionRate = parseFloat(item.commissionRate || 0);
      if (commissionRate <= 0) continue;

      const price = parseFloat(item.price || 0);
      const quantity = parseInt(item.quantity || 0);
      const amount = calculateProductCommission(price, quantity, commissionRate);

      if (amount <= 0) continue;

      // Create commission record
      const commissionRef = db.collection('commissions').doc();
      const commissionData = {
        OrderId: orderId,
        AffiliateId: affiliateId,
        CustomerId: customerId,
        ProductId: item.productId,
        ProductName: item.name || item.title || '',
        Price: price,
        Quantity: quantity,
        CommissionRate: commissionRate,
        Amount: amount,
        IsPaid: false,
        CreatedAt: firebase.firestore.FieldValue.serverTimestamp()
      };

      await commissionRef.set(commissionData);

      commissions.push({
        id: commissionRef.id,
        ...commissionData
      });
    }

    console.log(`Created ${commissions.length} commission records`);
    return commissions;
  } catch (error) {
    console.error('Error creating commissions from order:', error);
    return [];
  }
}

// Export additional functions
window.CommissionService = {
  ...window.CommissionService,
  calculateProductCommission,
  createCommissionsFromOrder,
  getProductCommissionRate,
  calculateAffiliateDiscount,
  processAffiliatePurchase,
  getAllCommissions: async function (affiliateId) {
    try {
      const snapshot = await firebaseService.db
        .collection('commissions')
        .where('AffiliateId', '==', affiliateId)
        .orderBy('CreatedAt', 'desc')
        .get();

      return snapshot.docs.map(doc => ({
        commissionId: doc.id,
        ...doc.data(),
        formattedDate: doc.data().CreatedAt ? doc.data().CreatedAt.toDate().toLocaleDateString() : 'N/A'
      }));
    } catch (error) {
      console.error('Error getting commissions:', error);
      return [];
    }
  },

  markAsPaid: async function (commissionId) {
    try {
      await firebaseService.db
        .collection('commissions')
        .doc(commissionId)
        .update({
          IsPaid: true,
          PaidDate: firebase.firestore.Timestamp.fromDate(new Date())
        });
      return true;
    } catch (error) {
      console.error('Error marking commission as paid:', error);
      return false;
    }
  },

  getCommissionSummary: async function (affiliateId) {
    try {
      const commissions = await this.getAllCommissions(affiliateId);

      const total = commissions.reduce((sum, commission) => sum + commission.Amount, 0);
      const paid = commissions.filter(c => c.IsPaid).reduce((sum, commission) => sum + commission.Amount, 0);
      const unpaid = total - paid;

      return { total, paid, unpaid };
    } catch (error) {
      console.error('Error getting commission summary:', error);
      return { total: 0, paid: 0, unpaid: 0 };
    }
  }
};
