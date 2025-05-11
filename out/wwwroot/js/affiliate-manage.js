/**
 * Affiliate Management
 */

'use strict';

// DataTable initialization
$(function () {
  let dt_affiliate_table = $('.datatable-affiliates');

  // DataTable with buttons
  if (dt_affiliate_table.length) {
    var dt_affiliate = dt_affiliate_table.DataTable({
      dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
      lengthMenu: [10, 25, 50, 75, 100],
      // Use pagingType for pagination type
      pagingType: 'simple_numbers',
      // For responsive
      responsive: true,
      language: {
        search: '',
        searchPlaceholder: 'Search affiliates'
      }
    });
  }

  // Filter form control to default size
  $('.dataTables_filter .form-control').removeClass('form-control-sm');
  $('.dataTables_length .form-select').removeClass('form-select-sm');
});

// Edit affiliate details - Function would fetch and populate data in a real application
function editAffiliate(affiliateId) {
  // In a real application, you would make an AJAX request to get affiliate data
  fetch(`/api/affiliates/${affiliateId}`)
    .then(response => response.json())
    .then(data => {
      // Populate the form with the affiliate data
      document.getElementById('affiliateId').value = data.affiliateId;
      document.getElementById('firstName').value = data.user.firstName;
      document.getElementById('lastName').value = data.user.lastName;
      document.getElementById('email').value = data.user.email;
      document.getElementById('phone').value = data.user.phoneNumber;
      document.getElementById('commissionRate').value = data.commissionRate;

      // Show the modal
      $('#editAffiliateModal').modal('show');
    })
    .catch(error => {
      console.error('Error fetching affiliate data:', error);

      // Show error message
      Swal.fire({
        title: 'Error!',
        text: 'Could not retrieve affiliate information. Please try again.',
        icon: 'error',
        customClass: {
          confirmButton: 'btn btn-primary'
        },
        buttonsStyling: false
      });
    });
}

// Delete affiliate with confirmation
function deleteAffiliate(affiliateId, affiliateName) {
  Swal.fire({
    title: 'Are you sure?',
    text: `You are about to delete ${affiliateName} from the affiliate program. This action cannot be undone!`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Yes, delete it!',
    cancelButtonText: 'No, cancel',
    customClass: {
      confirmButton: 'btn btn-danger me-3',
      cancelButton: 'btn btn-label-secondary'
    },
    buttonsStyling: false
  }).then(function (result) {
    if (result.isConfirmed) {
      // Make API request to delete affiliate
      fetch(`/api/affiliates/${affiliateId}`, {
        method: 'DELETE'
      })
        .then(response => {
          if (response.ok) {
            Swal.fire({
              title: 'Deleted!',
              text: `${affiliateName} has been removed from the affiliate program.`,
              icon: 'success',
              customClass: {
                confirmButton: 'btn btn-success'
              },
              buttonsStyling: false
            }).then(() => {
              // Reload the page to refresh the affiliate list
              location.reload();
            });
          } else {
            throw new Error('Failed to delete affiliate');
          }
        })
        .catch(error => {
          console.error('Error deleting affiliate:', error);

          Swal.fire({
            title: 'Error!',
            text: 'Failed to delete the affiliate. Please try again.',
            icon: 'error',
            customClass: {
              confirmButton: 'btn btn-primary'
            },
            buttonsStyling: false
          });
        });
    }
  });
}

// View commissions for a specific affiliate
function viewCommissions(affiliateId) {
  window.location.href = `/Affiliate/Commissions?affiliateId=${affiliateId}`;
}
