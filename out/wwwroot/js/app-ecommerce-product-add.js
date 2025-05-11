/**
 * Product Add/Edit
 * This script handles adding and editing products directly with Firestore
 */
document.addEventListener('DOMContentLoaded', function () {
  'use strict';

  console.log("DOM loaded, initializing product add/edit page...");

  // Initialize Firebase
  if (!firebase.apps.length) {
    try {
      firebase.initializeApp(firebaseConfig);
      console.log("Firebase initialized successfully");
    } catch (error) {
      console.error("Firebase initialization error:", error);
      toastr.error('Firebase initialization failed. Please check console for details.', 'Error');
      return;
    }
  }

  const db = firebase.firestore();
  const storage = firebase.storage();
  const productsCollection = db.collection('products');
  const brandsCollection = db.collection('brands');
  const categoriesCollection = db.collection('categories');

  // DOM elements
  const productForm = document.getElementById('productForm');
  const brandSelect = document.getElementById('BrandId');
  const categorySelect = document.getElementById('CategoryId');
  const saveProductBtn = document.querySelector('#saveProductBtn');
  const imagesJsonInput = document.getElementById('imagesJson');

  // Get product ID from URL if editing
  const urlParams = new URLSearchParams(window.location.search);
  const productId = urlParams.get('id');
  const isEditMode = !!productId;

  console.log("Mode:", isEditMode ? "Edit (ID: " + productId + ")" : "Add New");

  // Configure toastr notifications
  toastr.options = {
    closeButton: true,
    newestOnTop: true,
    progressBar: true,
    positionClass: 'toast-top-right',
    preventDuplicates: false,
    onclick: null,
    showDuration: '300',
    hideDuration: '1000',
    timeOut: '5000',
    extendedTimeOut: '1000',
    showEasing: 'swing',
    hideEasing: 'linear',
    showMethod: 'fadeIn',
    hideMethod: 'fadeOut'
  };

  // Variables to store data
  let brandsData = [];
  let categoriesData = [];
  let selectedBrand = null;
  let selectedCategory = null;
  let uploadedImages = [];

  // Initialize Dropzone
  let myDropzone = new Dropzone("#productImages", {
    url: "#", // We'll handle uploads manually
    autoProcessQueue: false,
    addRemoveLinks: true,
    maxFilesize: 5, // MB
    maxFiles: 5,
    acceptedFiles: "image/*",
    dictDefaultMessage: "Drop files here or click to upload images",
    dictRemoveFile: "Remove",
    clickable: true,
    createImageThumbnails: true,
    init: function () {
      console.log("Dropzone initialized");

      this.on("addedfile", function (file) {
        console.log("File added to dropzone:", file.name);
      });

      this.on("removedfile", function (file) {
        console.log("File removed from dropzone:", file.name);

        // Remove file from uploadedImages array if it was already uploaded
        if (file.serverUrl) {
          const index = uploadedImages.findIndex(url => url === file.serverUrl);
          if (index !== -1) {
            uploadedImages.splice(index, 1);
            updateImagesJson();

            // Delete from Firebase Storage if needed
            if (file.serverUrl && file.serverUrl.includes('firebasestorage')) {
              try {
                // Extract the path from the URL
                const fileUrl = new URL(file.serverUrl);
                const storagePath = decodeURIComponent(fileUrl.pathname.split('/o/')[1].split('?')[0]);

                // Delete the file
                storage.ref(storagePath).delete().then(() => {
                  console.log("File deleted from storage");
                }).catch(error => {
                  console.error("Error deleting file:", error);
                });
              } catch (error) {
                console.error("Error parsing file URL:", error);
              }
            }
          }
        }
      });
    }
  });

  // Update the hidden input with the JSON string of uploaded images
  function updateImagesJson() {
    imagesJsonInput.value = JSON.stringify(uploadedImages);
    console.log("Updated images JSON:", imagesJsonInput.value);
  }

  // Load brands from Firestore
  function loadBrands() {
    console.log("Loading brands from Firestore...");

    return brandsCollection.get().then(snapshot => {
      console.log("Brands snapshot received, count:", snapshot.size);

      brandsData = [];
      brandSelect.innerHTML = '<option value="">Select Brand</option>';

      snapshot.forEach(doc => {
        console.log("Brand document:", doc.id, doc.data());

        const brand = {
          id: doc.id,
          path: `brands/${doc.id}`, // Format without leading slash
          name: doc.data().Name || doc.data().name || 'Unnamed Brand'
        };
        brandsData.push(brand);

        const option = document.createElement('option');
        option.value = brand.path; // Use the path without leading slash
        option.textContent = brand.name;
        brandSelect.appendChild(option);

        console.log(`Added brand option: ${brand.name} with value: ${brand.path}`);
      });

      console.log("Brands loaded:", brandsData.length);

      // If in edit mode and we have a brand, select it
      if (isEditMode && selectedBrand) {
        console.log("Setting selected brand:", selectedBrand);
        brandSelect.value = selectedBrand;
        $(brandSelect).trigger('change');
      }

      return brandsData;
    }).catch(error => {
      console.error("Error loading brands:", error);
      toastr.error('Failed to load brands. Please try again.', 'Error');
    });
  }

 /* // Load categories for a specific brand
  function loadCategoriesByBrand(brandPath) {
    console.log("Loading categories for brand path:", brandPath);

    if (!brandPath) {
      console.log("No brand path provided, showing 'Select Brand First' message");
      categorySelect.innerHTML = '<option value="">Select Brand First</option>';
      $(categorySelect).trigger('change');
      return Promise.resolve([]);
    }

    // Extract the brand ID from the path
    const brandId = brandPath.split('/').pop();
    console.log("Extracted brand ID:", brandId);

    // Get all categories
    return categoriesCollection.get()
      .then(snapshot => {
        console.log(`Fetched all ${snapshot.size} categories to filter manually`);

        categoriesData = [];
        categorySelect.innerHTML = '<option value="">Select Category</option>';

        snapshot.forEach(doc => {
          const data = doc.data();
          console.log("Examining category:", doc.id, data);

          // Get the BrandId reference
          const categoryBrandId = data.BrandId;

          // Check if the reference points to our brand
          let isMatch = false;

          if (categoryBrandId) {
            // If it's a reference type with id property
            if (categoryBrandId.id === brandId) {
              isMatch = true;
              console.log("Match found by reference ID");
            }
            // If it's a reference with path property
            else if (categoryBrandId.path && categoryBrandId.path.includes(brandId)) {
              isMatch = true;
              console.log("Match found by reference path");
            }
            // If it has a _delegate property (Firestore reference)
            else if (categoryBrandId._delegate &&
              categoryBrandId._delegate.id === brandId) {
              isMatch = true;
              console.log("Match found by _delegate.id");
            }
            // If it's a string path that contains our brand ID
            else if (typeof categoryBrandId === 'string' &&
              (categoryBrandId === brandPath ||
                categoryBrandId.includes(brandId))) {
              isMatch = true;
              console.log("Match found by string path");
            }
          }

          if (isMatch) {
            const category = {
              id: doc.id,
              path: `/categories/${doc.id}`,
              name: data.Name || data.name || 'Unnamed Category'
            };

            categoriesData.push(category);

            const option = document.createElement('option');
            option.value = category.path;
            option.textContent = category.name;
            categorySelect.appendChild(option);
          }
        });

        if (categoriesData.length === 0) {
          console.log("No categories found for brand:", brandPath);
          categorySelect.innerHTML = '<option value="">No Categories Available</option>';
        } else {
          console.log(`Found ${categoriesData.length} categories for brand ${brandPath}`);
        }

        // Refresh Select2
        $(categorySelect).trigger('change');

        return categoriesData;
      })
      .catch(error => {
        console.error("Error loading categories:", error);
        toastr.error('Failed to load categories. Please try again.', 'Error');
        return [];
      });
  }*/

  // Load categories for a specific brand
  function loadCategoriesByBrand(brandPath) {
    console.log("Loading categories for brand path:", brandPath);

    if (!brandPath) {
      console.log("No brand path provided, showing 'Select Brand First' message");
      categorySelect.innerHTML = '<option value="">Select Brand First</option>';
      $(categorySelect).trigger('change');
      return Promise.resolve([]);
    }

    // Extract the brand ID from the path
    const brandId = brandPath.split('/').pop();
    console.log("Extracted brand ID:", brandId);

    // Get all categories
    return categoriesCollection.get()
      .then(snapshot => {
        console.log(`Fetched all ${snapshot.size} categories to filter manually`);

        categoriesData = [];
        categorySelect.innerHTML = '<option value="">Select Category</option>';

        snapshot.forEach(doc => {
          const data = doc.data();
          console.log("Examining category:", doc.id, data);

          // Get the BrandId reference
          const categoryBrandId = data.BrandId;

          // Check if the reference points to our brand
          let isMatch = false;

          if (categoryBrandId) {
            // If it's a reference type with id property
            if (categoryBrandId.id === brandId) {
              isMatch = true;
              console.log("Match found by reference ID");
            }
            // If it's a reference with path property
            else if (categoryBrandId.path && categoryBrandId.path.includes(brandId)) {
              isMatch = true;
              console.log("Match found by reference path");
            }
            // If it has a _delegate property (Firestore reference)
            else if (categoryBrandId._delegate &&
              categoryBrandId._delegate.id === brandId) {
              isMatch = true;
              console.log("Match found by _delegate.id");
            }
            // If it's a string path that contains our brand ID
            else if (typeof categoryBrandId === 'string' &&
              (categoryBrandId === brandPath ||
                categoryBrandId.includes(brandId))) {
              isMatch = true;
              console.log("Match found by string path");
            }
          }

          if (isMatch) {
            const category = {
              id: doc.id,
              path: `/categories/${doc.id}`,
              name: data.Name || data.name || 'Unnamed Category'
            };

            categoriesData.push(category);

            const option = document.createElement('option');
            option.value = category.path;
            option.textContent = category.name;
            categorySelect.appendChild(option);
          }
        });

        if (categoriesData.length === 0) {
          console.log("No categories found for brand:", brandPath);
          categorySelect.innerHTML = '<option value="">No Categories Available</option>';
        } else {
          console.log(`Found ${categoriesData.length} categories for brand ${brandPath}`);
        }

        // Refresh Select2
        $(categorySelect).trigger('change');

        return categoriesData;
      })
      .catch(error => {
        console.error("Error loading categories:", error);
        toastr.error('Failed to load categories. Please try again.', 'Error');
        return [];
      });
  }

 /* // Add this to your code right after initializing the brandSelect variable
  console.log("Setting up brand selection change event handler");
  brandSelect.addEventListener('change', function () {
    const brandPath = this.value;
    console.log("Brand selection changed to:", brandPath);

    if (brandPath) {
      loadCategoriesByBrand(brandPath);
    } else {
      categorySelect.innerHTML = '<option value="">Select Brand First</option>';
      $(categorySelect).trigger('change');
    }
  });*/
  // Set up brand selection change event handler (keep this as a backup)
  console.log("Setting up brand selection change event handler");
  brandSelect.addEventListener('change', function () {
    const brandPath = this.value;
    console.log("Brand native change event, selected:", brandPath);

    if (brandPath) {
      loadCategoriesByBrand(brandPath);
    } else {
      categorySelect.innerHTML = '<option value="">Select Brand First</option>';
      $(categorySelect).trigger('change');
    }
  });



 /* // Initialize Select2 with proper events
  function initializeSelect2() {
    try {
      $('.select2').select2({
        placeholder: 'Select an option',
        width: '100%'
      });

      console.log("Select2 initialized");

      // Make sure Select2 change events propagate to the native select element
      $('.select2').on('select2:select', function (e) {
        console.log("Select2 selection changed:", e.params.data);
        $(this).trigger('change');
      });
    } catch (error) {
      console.error("Select2 initialization error:", error);
    }
  }*/
  // Initialize Select2 with proper events
  // Initialize Select2 with proper events
  function initializeSelect2() {
    try {
      $('.select2').select2({
        placeholder: 'Select an option',
        width: '100%'
      });
      console.log("Select2 initialized");

      // For brand select specifically, add a direct event handler
      $('#BrandId').on('select2:select', function (e) {
        console.log("Brand select2:select event triggered");
        const brandPath = e.params.data.id;
        console.log("Selected brand path:", brandPath);

        if (brandPath) {
          loadCategoriesByBrand(brandPath);
        } else {
          categorySelect.innerHTML = '<option value="">Select Brand First</option>';
          $(categorySelect).trigger('change');
        }
      });
    } catch (error) {
      console.error("Select2 initialization error:", error);
    }
  }



  // Upload images to Firebase Storage
  async function uploadImages(brandPath, categoryPath) {
    console.log("Starting image upload process...");
    console.log("Files in dropzone:", myDropzone.files.length);

    if (!myDropzone.files || myDropzone.files.length === 0) {
      console.log("No files to upload");
      return [];
    }

    // Extract brand and category names for folder structure
    const brandName = brandPath ? brandsData.find(b => b.path === brandPath)?.name : 'Unknown';
    const categoryName = categoryPath ? categoriesData.find(c => c.path === categoryPath)?.name : 'Unknown';

    // Create folder path (format: BrandName/CategoryName)
    const folderPath = `${brandName}/${categoryName.replace(/\s+/g, '')}`;
    console.log("Upload folder path:", folderPath);

    const uploadPromises = [];

    for (const file of myDropzone.files) {
      // Skip if already uploaded
      if (file.serverUrl) {
        console.log("Skipping already uploaded file:", file.name);
        continue;
      }

      const fileExtension = file.name.split('.').pop();
      const fileName = `${brandName.substring(0, 1)}-${categoryName.substring(0, 2)}-${file.name.split('.')[0]}.${fileExtension}`;
      const filePath = `${folderPath}/${fileName}`;

      console.log(`Uploading file to path: ${filePath}`);

      // Create a storage reference
      const storageRef = storage.ref(filePath);

      // Create upload task
      const uploadTask = storageRef.put(file);

      // Create promise for this upload
      const uploadPromise = new Promise((resolve, reject) => {
        uploadTask.on('state_changed',
          (snapshot) => {
            // Progress function
            const progress = (snapshot.bytesTransferred / snapshot.totalBytes) * 100;
            console.log(`Upload is ${progress}% done`);
            file.upload.progress = progress / 100;
            myDropzone.emit("uploadprogress", file, progress, 100);
          },
          (error) => {
            // Error function
            console.error("Upload error:", error);
            reject(error);
          },
          async () => {
            // Complete function
            try {
              const downloadURL = await uploadTask.snapshot.ref.getDownloadURL();
              console.log("File uploaded successfully, download URL:", downloadURL);
              file.serverUrl = downloadURL;
              uploadedImages.push(downloadURL);
              resolve(downloadURL);
            } catch (error) {
              console.error("Error getting download URL:", error);
              reject(error);
            }
          }
        );
      });

      uploadPromises.push(uploadPromise);
    }

    try {
      await Promise.all(uploadPromises);
      updateImagesJson();
      console.log("All images uploaded successfully");
      return uploadedImages;
    } catch (error) {
      console.error("Error uploading images:", error);
      toastr.error('Some images failed to upload. Please try again.', 'Upload Error');
      return uploadedImages; // Return what was successfully uploaded
    }
  }

  // Load product data if in edit mode
  async function loadProductData() {
    if (!isEditMode) return;

    console.log("Loading product data for ID:", productId);

    try {
      const docRef = productsCollection.doc(productId);
      const doc = await docRef.get();

      if (!doc.exists) {
        console.error("Product not found:", productId);
        toastr.error('Product not found', 'Error');
        setTimeout(() => {
          window.location.href = '/Ecommerce/ProductList';
        }, 2000);
        return;
      }

      const productData = doc.data();
      console.log("Product data loaded:", productData);

      // Set form values
      document.getElementById('Name').value = productData.Name || '';
      document.getElementById('SKU').value = productData.SKU || '';
      document.getElementById('Description').value = productData.Description || '';
      document.getElementById('Price').value = productData.Price || 0;
      document.getElementById('Stock').value = productData.Stock || 0;
      document.getElementById('Discount').value = productData.Discount || 0;
      document.getElementById('Commission').value = productData.Commission || 30;

      // Store references for later selection after data is loaded
      selectedBrand = productData.BrandId || null;
      selectedCategory = productData.CategoryId || null;

      console.log("Selected brand:", selectedBrand);
      console.log("Selected category:", selectedCategory);

      // Load images
      if (productData.Image && Array.isArray(productData.Image)) {
        uploadedImages = [...productData.Image];
        updateImagesJson();

        console.log("Loading existing images:", uploadedImages);

        // Add existing images to dropzone
        for (const imageUrl of productData.Image) {
          const mockFile = { name: imageUrl.split('/').pop(), size: 0, serverUrl: imageUrl };
          myDropzone.emit("addedfile", mockFile);
          myDropzone.emit("thumbnail", mockFile, imageUrl);
          myDropzone.emit("complete", mockFile);
          myDropzone.files.push(mockFile);
        }
      }

      // Update page title and button text
      document.title = 'Edit Product - eCommerce';
      document.querySelector('.card-title').textContent = 'Edit Product';
      saveProductBtn.textContent = 'Update Product';

    } catch (error) {
      console.error("Error loading product:", error);
      toastr.error('Failed to load product data', 'Error');
    }
  }

  // Add test button for category loading
  function addTestButton() {
    console.log("Adding test button for category loading");

    const testButton = document.createElement('button');
    testButton.type = 'button';
    testButton.className = 'btn btn-sm btn-info mb-3';
    testButton.textContent = 'Test Load Categories';
    testButton.style.marginLeft = '10px';

    testButton.onclick = function () {
      const selectedBrandPath = $('#BrandId').val();
      console.log("Test button clicked, selected brand:", selectedBrandPath);

      if (selectedBrandPath) {
        loadCategoriesByBrand(selectedBrandPath);
      } else {
        alert('Please select a brand first');
      }
    };

    // Add the button after the brand select
    const brandFormGroup = document.querySelector('#BrandId').closest('.mb-3');
    brandFormGroup.appendChild(testButton);
  }

  // Validate form
  function validateForm() {
    const name = document.getElementById('Name').value.trim();
    const price = parseFloat(document.getElementById('Price').value);
    const stock = parseInt(document.getElementById('Stock').value);
    const brandId = document.getElementById('BrandId').value;
    const categoryId = document.getElementById('CategoryId').value;

    let isValid = true;
    let errorMessage = '';

    // Reset validation
    document.querySelectorAll('.is-invalid').forEach(el => {
      el.classList.remove('is-invalid');
    });

    // Validate required fields
    if (!name) {
      document.getElementById('Name').classList.add('is-invalid');
      errorMessage = 'Product name is required';
      isValid = false;
    }

    if (isNaN(price) || price <= 0) {
      document.getElementById('Price').classList.add('is-invalid');
      errorMessage = errorMessage || 'Valid price is required';
      isValid = false;
    }

    if (isNaN(stock) || stock < 0) {
      document.getElementById('Stock').classList.add('is-invalid');
      errorMessage = errorMessage || 'Valid stock quantity is required';
      isValid = false;
    }

    if (!brandId) {
      document.getElementById('BrandId').classList.add('is-invalid');
      errorMessage = errorMessage || 'Brand selection is required';
      isValid = false;
    }

    if (!categoryId) {
      document.getElementById('CategoryId').classList.add('is-invalid');
      errorMessage = errorMessage || 'Category selection is required';
      isValid = false;
    }

    if (!isValid && errorMessage) {
      toastr.error(errorMessage, 'Validation Error');
    }

    return isValid;
  }

  // Save product to Firestore
  async function saveProduct(event) {
    event.preventDefault();

    console.log("Save product form submitted");

    if (!validateForm()) {
      console.log("Form validation failed");
      return;
    }

    // Disable save button to prevent multiple submissions
    saveProductBtn.disabled = true;
    saveProductBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';

    try {
      // Get form values
      const name = document.getElementById('Name').value.trim();
      const sku = document.getElementById('SKU').value.trim();
      const description = document.getElementById('Description').value.trim();
      const price = parseFloat(document.getElementById('Price').value);
      const stock = parseInt(document.getElementById('Stock').value);
      const discount = parseInt(document.getElementById('Discount').value) || 0;
      const commission = parseInt(document.getElementById('Commission').value) || 30;
      const brandId = document.getElementById('BrandId').value;
      const categoryId = document.getElementById('CategoryId').value;

      console.log("Form values:", { name, sku, price, stock, discount, commission, brandId, categoryId });

      // Upload images first
      await uploadImages(brandId, categoryId);

      // Prepare product data
      const productData = {
        Name: name,
        SKU: sku,
        Description: description,
        Price: price,
        Stock: stock,
        Discount: discount,
        Commission: commission,
        BrandId: brandId,
        CategoryId: categoryId,
        Image: uploadedImages,
        UpdatedAt: firebase.firestore.FieldValue.serverTimestamp()
      };

      console.log("Product data to save:", productData);

      if (isEditMode) {
        // Update existing product
        await productsCollection.doc(productId).update(productData);
        console.log("Product updated successfully");
        toastr.success('Product updated successfully', 'Success');
      } else {
        // Add new product
        productData.CreatedAt = firebase.firestore.FieldValue.serverTimestamp();
        const docRef = await productsCollection.add(productData);

        // Add the ProductId field to the document
        await docRef.update({
          ProductId: docRef.id
        });

        console.log("Product added successfully with ID:", docRef.id);
        toastr.success('Product added successfully', 'Success');
      }

      // Redirect after short delay
      setTimeout(() => {
        window.location.href = '/Ecommerce/ProductList';
      }, 1500);

    } catch (error) {
      console.error("Error saving product:", error);
      toastr.error('Failed to save product: ' + error.message, 'Error');

      // Re-enable save button
      saveProductBtn.disabled = false;
      saveProductBtn.textContent = isEditMode ? 'Update Product' : 'Save Product';
    }
  }

 /* // Initialize the page
  async function initPage() {
    try {
      console.log("Initializing product page...");

      // Initialize Select2
      initializeSelect2();

      // Load brands first
      await loadBrands();

      // Add test button for debugging
      addTestButton();

      // If editing, load product data
      if (isEditMode) {
        await loadProductData();
      }

      // Set up form submission
      productForm.addEventListener('submit', saveProduct);

    } catch (error) {
      console.error("Error initializing page:", error);
      toastr.error('Failed to initialize page. Please refresh and try again.', 'Error');
    }
  }*/
  // Initialize the page
  async function initPage() {
    try {
      console.log("Initializing product page...");

      // Initialize Select2
      initializeSelect2();

      // Load brands first
      await loadBrands();

      // If editing, load product data
      if (isEditMode) {
        await loadProductData();
      }

      // Set up form submission
      productForm.addEventListener('submit', saveProduct);

    } catch (error) {
      console.error("Error initializing page:", error);
      toastr.error('Failed to initialize page. Please refresh and try again.', 'Error');
    }
  }

  // Start initialization
  initPage();
});

