/**
 * Page User List
 */

// Update the DataTable initialization to use your API
if (dt_user_table.length) {
  var dt_user = dt_user_table.DataTable({
    ajax: '/Users/GetUsers', // Use your controller endpoint
    columns: [
      // columns according to JSON
      { data: '' }, // For responsive control
      { data: 'userId' }, // For checkboxes
      { data: '' }, // For user info (custom render)
      { data: 'role' },
      { data: '' }, // For plan (not used)
      { data: '' }, // For billing (not used)
      { data: '' }, // For status (not used)
      { data: '' } // For actions (custom render)
    ],
    columnDefs: [
      // For Responsive
      {
        className: 'control',
        searchable: false,
        orderable: false,
        responsivePriority: 2,
        targets: 0,
        render: function () {
          return '';
        }
      },
      // For Checkboxes
      {
        targets: 1,
        orderable: false,
        checkboxes: {
          selectAllRender: '<input type="checkbox" class="form-check-input">'
        },
        render: function (data) {
          return '<input type="checkbox" class="dt-checkboxes form-check-input" value="' + data + '">';
        },
        searchable: false
      },
      // User full name and email
      {
        targets: 2,
        responsivePriority: 4,
        render: function (data, type, full) {
          var $name = full['firstName'] + ' ' + (full['middleName'] ? full['middleName'] + ' ' : '') + full['lastName'],
            $email = full['email'],
            $image = ''; // No image in your data model

          // For Avatar badge (using initials)
          var stateNum = Math.floor(Math.random() * 6);
          var states = ['success', 'danger', 'warning', 'info', 'primary', 'secondary'];
          var $state = states[stateNum],
            $initials = $name.match(/\b\w/g) || [];
          $initials = (($initials.shift() || '') + ($initials.pop() || '')).toUpperCase();

$output = '<span class="avatar-initial rounded-circle bg-label-' + $state + '">' + $initials + '</span>';

// Creates full output for row
var $row_output =
  '<div class="d-flex justify-content-start align-items-center user-name">' +
  '<div class="avatar-wrapper">' +
  '<div class="avatar avatar-sm me-4">' +
  $output +
  '</div>' +
  '</div>' +
  '<div class="d-flex flex-column">' +
  '<a href="/Users/ViewAccount/' + full['userId'] + '" class="text-heading text-truncate"><span class="fw-medium">' +
  $name +
  '</span></a>' +
  '<small>' +
  $email +
  '</small>' +
  '</div>' +
  '</div>';
return $row_output;
        }
      },
// User Role
{
  targets: 3,
    render: function (data) {
      var roleBadgeObj = {
        'Admin': '<i class="ti ti-device-desktop ti-md text-danger me-2"></i>',
        'Affiliate': '<i class="ti ti-chart-pie ti-md text-info me-2"></i>',
        'Customer': '<i class="ti ti-user ti-md text-success me-2"></i>'
      };

      var icon = roleBadgeObj[data] || '<i class="ti ti-user ti-md text-primary me-2"></i>';
      return (
        "<span class='text-truncate d-flex align-items-center text-heading'>" +
        icon +
        data +
        '</span>'
      );
    }
      },
/*
// Plans (placeholder)
{
  targets: 4,
    render: function () {
      return '<span class="text-heading">Basic</span>';
    }
},
// Billing (placeholder)
{
  targets: 5,
    render: function () {
      return '<span class="text-heading">Auto Debit</span>';
    }
},
// User Status (placeholder)
{
  targets: 6,
    render: function () {
      return '<span class="badge bg-label-success">Active</span>';
    }
},
*/
// Actions
{
  targets: -1,
    title: 'Actions',
      searchable: false,
        orderable: false,
          render: function (data, type, full) {
            return (
              '<div class="d-flex align-items-center">' +
              '<a href="javascript:;" data-id="' + full['userId'] + '" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill delete-record"><i class="ti ti-trash ti-md"></i></a>' +
              '<a href="/Users/ViewAccount/' + full['userId'] + '" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill"><i class="ti ti-eye ti-md"></i></a>' +
              '<a href="javascript:;" onclick="loadUserForEdit(\'' + full['userId'] + '\')" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill"><i class="ti ti-edit ti-md"></i></a>' +
              '<a href="javascript:;" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-md"></i></a>' +
              '<div class="dropdown-menu dropdown-menu-end m-0">' +
              '<a href="javascript:;" onclick="loadUserForEdit(\'' + full['userId'] + '\')" class="dropdown-item">Edit</a>' +
              '<a href="javascript:;" class="dropdown-item">Suspend</a>' +
              '</div>' +
              '</div>'
            );
          }
}

    ],
order: [[2, 'desc']],
  dom:
'<"row"' +
  '<"col-md-2"<"ms-n2"l>>' +
  '<"col-md-10"<"dt-action-buttons text-xl-end text-lg-start text-md-end text-start d-flex align-items-center justify-content-end flex-md-row flex-column mb-6 mb-md-0 mt-n6 mt-md-0"fB>>' +
  '>t' +
  '<"row"' +
  '<"col-sm-12 col-md-6"i>' +
  '<"col-sm-12 col-md-6"p>' +
  '>',
  language: {
  sLengthMenu: '_MENU_',
    search: '',
      searchPlaceholder: 'Search User',
        paginate: {
    next: '<i class="ti ti-chevron-right ti-sm"></i>',
      previous: '<i class="ti ti-chevron-left ti-sm"></i>'
  }
},
// Buttons with Dropdown
buttons: [
  {
    extend: 'collection',
    className: 'btn btn-label-secondary dropdown-toggle mx-4 waves-effect waves-light',
    text: '<i class="ti ti-upload me-2 ti-xs"></i>Export',
    buttons: [
      {
        extend: 'print',
        text: '<i class="ti ti-printer me-2" ></i>Print',
        className: 'dropdown-item',
        exportOptions: {
          columns: [1, 2, 3, 4, 5],
          // prevent avatar to be print
          format: {
            body: function (inner, coldex, rowdex) {
              if (inner.length <= 0) return inner;
              var el = $.parseHTML(inner);
              var result = '';
              $.each(el, function (index, item) {
                if (item.classList !== undefined && item.classList.contains('user-name')) {
                  result = result + item.lastChild.firstChild.textContent;
                } else if (item.innerText === undefined) {
                  result = result + item.textContent;
                } else result = result + item.innerText;
              });
              return result;
            }
          }
        },
        customize: function (win) {
          //customize print view for dark
          $(win.document.body)
            .css('color', headingColor)
            .css('border-color', borderColor)
            .css('background-color', bodyBg);
          $(win.document.body)
            .find('table')
            .addClass('compact')
            .css('color', 'inherit')
            .css('border-color', 'inherit')
            .css('background-color', 'inherit');
        }
      },
      /*
      {
        extend: 'csv',
        text: '<i class="ti ti-file-text me-2" ></i>Csv',
        className: 'dropdown-item',
        exportOptions: {
          columns: [1, 2, 3, 4, 5],
          // prevent avatar to be display
          format: {
            body: function (inner, coldex, rowdex) {
              if (inner.length <= 0) return inner;
              var el = $.parseHTML(inner);
              var result = '';
              $.each(el, function (index, item) {
                if (item.classList !== undefined && item.classList.contains('user-name')) {
                  result = result + item.lastChild.firstChild.textContent;
                } else if (item.innerText === undefined) {
                  result = result + item.textContent;
                } else result = result + item.innerText;
              });
              return result;
            }
          }
        }
      },*/
      {
        extend: 'excel',
        text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
        className: 'dropdown-item',
        exportOptions: {
          columns: [1, 2, 3, 4, 5],
          // prevent avatar to be display
          format: {
            body: function (inner, coldex, rowdex) {
              if (inner.length <= 0) return inner;
              var el = $.parseHTML(inner);
              var result = '';
              $.each(el, function (index, item) {
                if (item.classList !== undefined && item.classList.contains('user-name')) {
                  result = result + item.lastChild.firstChild.textContent;
                } else if (item.innerText === undefined) {
                  result = result + item.textContent;
                } else result = result + item.innerText;
              });
              return result;
            }
          }
        }
      },/*
      {
        extend: 'pdf',
        text: '<i class="ti ti-file-code-2 me-2"></i>Pdf',
        className: 'dropdown-item',
        exportOptions: {
          columns: [1, 2, 3, 4, 5],
          // prevent avatar to be display
          format: {
            body: function (inner, coldex, rowdex) {
              if (inner.length <= 0) return inner;
              var el = $.parseHTML(inner);
              var result = '';
              $.each(el, function (index, item) {
                if (item.classList !== undefined && item.classList.contains('user-name')) {
                  result = result + item.lastChild.firstChild.textContent;
                } else if (item.innerText === undefined) {
                  result = result + item.textContent;
                } else result = result + item.innerText;
              });
              return result;
            }
          }
        }
      },*/
      {
        extend: 'copy',
        text: '<i class="ti ti-copy me-2" ></i>Copy',
        className: 'dropdown-item',
        exportOptions: {
          columns: [1, 2, 3, 4, 5],
          // prevent avatar to be display
          format: {
            body: function (inner, coldex, rowdex) {
              if (inner.length <= 0) return inner;
              var el = $.parseHTML(inner);
              var result = '';
              $.each(el, function (index, item) {
                if (item.classList !== undefined && item.classList.contains('user-name')) {
                  result = result + item.lastChild.firstChild.textContent;
                } else if (item.innerText === undefined) {
                  result = result + item.textContent;
                } else result = result + item.innerText;
              });
              return result;
            }
          }
        }
      }
    ]
  },
  {
    text: '<i class="ti ti-plus me-0 me-sm-1 ti-xs"></i><span class="d-none d-sm-inline-block">Add New User</span>',
    className: 'add-new btn btn-primary waves-effect waves-light',
    attr: {
      'data-bs-toggle': 'offcanvas',
      'data-bs-target': '#offcanvasAddUser'
    }
  }
],
  // For responsive popup
  responsive: {
  details: {
    display: $.fn.dataTable.Responsive.display.modal({
      header: function (row) {
        var data = row.data();
        return 'Details of ' + data['firstName'] + ' ' + data['lastName'];
      }
    }),
      type: 'column',
        renderer: function (api, rowIdx, columns) {
          var data = $.map(columns, function (col, i) {
            return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
              ? '<tr data-dt-row="' +
              col.rowIndex +
              '" data-dt-column="' +
              col.columnIndex +
              '">' +
              '<td>' +
              col.title +
              ':' +
              '</td> ' +
              '<td>' +
              col.data +
              '</td>' +
              '</tr>'
              : '';
          }).join('');

          return data ? $('<table class="table"/><tbody />').append(data) : false;
        }
  }
},
initComplete: function () {
  // Adding role filter once table initialized
  this.api()
    .columns(3)
    .every(function () {
      var column = this;
      var select = $(
        '<select id="UserRole" class="form-select text-capitalize"><option value=""> Select Role </option></select>'
      )
        .appendTo('.user_role')
        .on('change', function () {
          var val = $.fn.dataTable.util.escapeRegex($(this).val());
          column.search(val ? '^' + val + '$' : '', true, false).draw();
        });

      column
        .data()
        .unique()
        .sort()
        .each(function (d, j) {
          select.append('<option value="' + d + '">' + d + '</option>');
        });
    });
}
  });
}


// Initialize edit user form
function initEditUserForm() {
  // Password toggle
  document.querySelectorAll('.toggle-password').forEach(toggle => {
    toggle.addEventListener('click', e => {
      const target = e.currentTarget.parentNode.querySelector('input');
      const type = target.getAttribute('type') === 'password' ? 'text' : 'password';
      target.setAttribute('type', type);

      // Toggle icon
      const icon = e.currentTarget.querySelector('i');
      if (type === 'password') {
        icon.classList.remove('ti-eye');
        icon.classList.add('ti-eye-off');
      } else {
        icon.classList.remove('ti-eye-off');
        icon.classList.add('ti-eye');
      }
    });
  });

  // Form submission
  const editUserForm = document.getElementById('editUserForm');
  if (editUserForm) {
    editUserForm.addEventListener('submit', function (e) {
      e.preventDefault();

      const formData = new FormData(editUserForm);
      const userId = formData.get('UserId');

      // Validate form
      if (!formData.get('FirstName') || !formData.get('LastName') || !formData.get('Email') || !formData.get('Role')) {
        alert('Please fill in all required fields');
        return;
      }

      // Convert FormData to JSON
      const data = {};
      formData.forEach((value, key) => {
        data[key] = value;
      });

      // Send AJAX request
      fetch('/Users/UpdateUser', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify(data)
      })
        .then(response => response.json())
        .then(result => {
          if (result.success) {
            // Close modal and refresh table
            $('#editUserModal').modal('hide');
            $('.datatables-users').DataTable().ajax.reload();

            // Show success message
            toastr.success('User updated successfully');
          } else {
            // Show error message
            toastr.error(result.message || 'Failed to update user');
          }
        })
        .catch(error => {
          console.error('Error:', error);
          toastr.error('An error occurred while updating the user');
        });
    });
  }
}

// Function to load user data for editing
function loadUserForEdit(userId) {
  fetch(`/Users/GetUserForEdit?id=${userId}`)
    .then(response => response.json())
    .then(result => {
      if (result.success) {
        const user = result.data;

        // Populate form fields
        document.getElementById('edit-user-id').value = user.userId;
        document.getElementById('edit-user-FirstName').value = user.firstName || '';
        document.getElementById('edit-user-MiddleName').value = user.middleName || '';
        document.getElementById('edit-user-LastName').value = user.lastName || '';
        document.getElementById('edit-user-email').value = user.email || '';
        document.getElementById('edit-user-PhoneNumber').value = user.phoneNumber || '';

        // Set select value
        const roleSelect = document.getElementById('edit-user-Role');
        if (roleSelect) {
          for (let i = 0; i < roleSelect.options.length; i++) {
            if (roleSelect.options[i].value === user.role) {
              roleSelect.selectedIndex = i;
              break;
            }
          }
        }

        // Clear password field
        document.getElementById('edit-user-password').value = '';

        // Show modal
        $('#editUserModal').modal('show');
      } else {
        toastr.error(result.message || 'Failed to load user data');
      }
    })
    .catch(error => {
      console.error('Error:', error);
      toastr.error('An error occurred while loading user data');
    });
}
/*
// Delete Record
$(document).on('click', '.delete-record', function () {
  var userId = $(this).data('id');
  if (confirm('Are you sure you want to delete this user?')) {
    $.ajax({
      url: '/Users/DeleteUser',
      type: 'POST',
      data: {
        id: userId,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
      },
      success: function (result) {
        $('.datatables-users').DataTable().ajax.reload();
        toastr.success('User deleted successfully');
      },
      error: function (error) {
        toastr.error('Error deleting user');
        console.error(error);
      }
    });
  }
});

// Add New User Form Submission
$(document).on('submit', '#addNewUserForm', function (e) {
  e.preventDefault();

  var formData = new FormData(this);

  $.ajax({
    url: '/Users/AddUser',
    type: 'POST',
    data: formData,
    processData: false,
    contentType: false,
    success: function (result) {
      $('#offcanvasAddUser').offcanvas('hide');
      $('.datatables-users').DataTable().ajax.reload();
      toastr.success('User added successfully');
      $('#addNewUserForm')[0].reset();
    },
    error: function (error) {
      toastr.error('Error adding user');
      console.error(error);
    }
  });
});

// Add to your app-user-list.js
// Initialize toastr
toastr.options = {
  closeButton: true,
  progressBar: true,
  positionClass: 'toast-top-right',
  timeOut: 3000
};
*/

'use strict';

// Datatable (jquery)
$(function () {
  let borderColor, bodyBg, headingColor;

  if (isDarkStyle) {
    borderColor = config.colors_dark.borderColor;
    bodyBg = config.colors_dark.bodyBg;
    headingColor = config.colors_dark.headingColor;
  } else {
    borderColor = config.colors.borderColor;
    bodyBg = config.colors.bodyBg;
    headingColor = config.colors.headingColor;
  }

  // Variable declaration for table
  var dt_user_table = $('.datatables-users'),
    select2 = $('.select2'),
    userView = '/Users/ViewAccount',
    statusObj = {
      1: { title: 'Pending', class: 'bg-label-warning' },
      2: { title: 'Active', class: 'bg-label-success' },
      3: { title: 'Inactive', class: 'bg-label-secondary' }
    };

  if (select2.length) {
    var $this = select2;
    $this.wrap('<div class="position-relative"></div>').select2({
      placeholder: 'Select Country',
      dropdownParent: $this.parent()
    });
  }

  // Users datatable
  if (dt_user_table.length) {
    var dt_user = dt_user_table.DataTable({
      ajax: assetsPath + 'json/user-list.json', // JSON file to add data
      columns: [
        // columns according to JSON
        { data: 'id' },
        { data: 'id' },
        { data: 'full_name' },
        { data: 'role' },
        { data: 'current_plan' },
        { data: 'billing' },
        { data: 'status' },
        { data: 'action' }
      ],
      columnDefs: [
        {
          // For Responsive
          className: 'control',
          searchable: false,
          orderable: false,
          responsivePriority: 2,
          targets: 0,
          render: function (data, type, full, meta) {
            return '';
          }
        },
        {
          // For Checkboxes
          targets: 1,
          orderable: false,
          checkboxes: {
            selectAllRender: '<input type="checkbox" class="form-check-input">'
          },
          render: function () {
            return '<input type="checkbox" class="dt-checkboxes form-check-input" >';
          },
          searchable: false
        },
        {
          // User full name and email
          targets: 2,
          responsivePriority: 4,
          render: function (data, type, full, meta) {
            var $name = full['full_name'],
              $email = full['email'],
              $image = full['avatar'];
            if ($image) {
              // For Avatar image
              var $output =
                '<img src="' + assetsPath + 'img/avatars/' + $image + '" alt="Avatar" class="rounded-circle">';
            } else {
              // For Avatar badge
              var stateNum = Math.floor(Math.random() * 6);
              var states = ['success', 'danger', 'warning', 'info', 'primary', 'secondary'];
              var $state = states[stateNum],
                $name = full['full_name'],
                $initials = $name.match(/\b\w/g) || [];
              $initials = (($initials.shift() || '') + ($initials.pop() || '')).toUpperCase();
              $output = '<span class="avatar-initial rounded-circle bg-label-' + $state + '">' + $initials + '</span>';
            }
            // Creates full output for row
            var $row_output =
              '<div class="d-flex justify-content-start align-items-center user-name">' +
              '<div class="avatar-wrapper">' +
              '<div class="avatar avatar-sm me-4">' +
              $output +
              '</div>' +
              '</div>' +
              '<div class="d-flex flex-column">' +
              '<a href="' +
              userView +
              '" class="text-heading text-truncate"><span class="fw-medium">' +
              $name +
              '</span></a>' +
              '<small>' +
              $email +
              '</small>' +
              '</div>' +
              '</div>';
            return $row_output;
          }
        },
        {
          // User Role
          targets: 3,
          render: function (data, type, full, meta) {
            var $role = full['role'];
            var roleBadgeObj = {
              Subscriber: '<i class="ti ti-crown ti-md text-primary me-2"></i>',
              Author: '<i class="ti ti-edit ti-md text-warning me-2"></i>',
              Maintainer: '<i class="ti ti-user ti-md text-success me-2"></i>',
              Editor: '<i class="ti ti-chart-pie ti-md text-info me-2"></i>',
              Admin: '<i class="ti ti-device-desktop ti-md text-danger me-2"></i>'
            };
            return (
              "<span class='text-truncate d-flex align-items-center text-heading'>" +
              roleBadgeObj[$role] +
              $role +
              '</span>'
            );
          }
        },
        {
          // Plans
          targets: 4,
          render: function (data, type, full, meta) {
            var $plan = full['current_plan'];

            return '<span class="text-heading">' + $plan + '</span>';
          }
        },
        {
          // User Status
          targets: 6,
          render: function (data, type, full, meta) {
            var $status = full['status'];

            return (
              '<span class="badge ' +
              statusObj[$status].class +
              '" text-capitalized>' +
              statusObj[$status].title +
              '</span>'
            );
          }
        },
        {
          // Actions
          targets: -1,
          title: 'Actions',
          searchable: false,
          orderable: false,
          render: function (data, type, full, meta) {
            return (
              '<div class="d-flex align-items-center">' +
              '<a href="javascript:;" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill delete-record"><i class="ti ti-trash ti-md"></i></a>' +
              '<a href="' +
              userView +
              '" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill"><i class="ti ti-eye ti-md"></i></a>' +
              '<a href="javascript:;" class="btn btn-icon btn-text-secondary waves-effect waves-light rounded-pill dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-md"></i></a>' +
              '<div class="dropdown-menu dropdown-menu-end m-0">' +
              '<a href="javascript:;"" class="dropdown-item">Edit</a>' +
              '<a href="javascript:;" class="dropdown-item">Suspend</a>' +
              '</div>' +
              '</div>'
            );
          }
        }
      ],
      order: [[2, 'desc']],
      dom:
        '<"row"' +
        '<"col-md-2"<"ms-n2"l>>' +
        '<"col-md-10"<"dt-action-buttons text-xl-end text-lg-start text-md-end text-start d-flex align-items-center justify-content-end flex-md-row flex-column mb-6 mb-md-0 mt-n6 mt-md-0"fB>>' +
        '>t' +
        '<"row"' +
        '<"col-sm-12 col-md-6"i>' +
        '<"col-sm-12 col-md-6"p>' +
        '>',
      language: {
        sLengthMenu: '_MENU_',
        search: '',
        searchPlaceholder: 'Search User',
        paginate: {
          next: '<i class="ti ti-chevron-right ti-sm"></i>',
          previous: '<i class="ti ti-chevron-left ti-sm"></i>'
        }
      },
      // Buttons with Dropdown
      buttons: [
        {
          extend: 'collection',
          className: 'btn btn-label-secondary dropdown-toggle mx-4 waves-effect waves-light',
          text: '<i class="ti ti-upload me-2 ti-xs"></i>Export',
          buttons: [
            {
              extend: 'print',
              text: '<i class="ti ti-printer me-2" ></i>Print',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be print
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              },
              customize: function (win) {
                //customize print view for dark
                $(win.document.body)
                  .css('color', headingColor)
                  .css('border-color', borderColor)
                  .css('background-color', bodyBg);
                $(win.document.body)
                  .find('table')
                  .addClass('compact')
                  .css('color', 'inherit')
                  .css('border-color', 'inherit')
                  .css('background-color', 'inherit');
              }
            },
            {
              extend: 'csv',
              text: '<i class="ti ti-file-text me-2" ></i>Csv',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            },
            {
              extend: 'excel',
              text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            },
            {
              extend: 'pdf',
              text: '<i class="ti ti-file-code-2 me-2"></i>Pdf',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            },
            {
              extend: 'copy',
              text: '<i class="ti ti-copy me-2" ></i>Copy',
              className: 'dropdown-item',
              exportOptions: {
                columns: [1, 2, 3, 4, 5],
                // prevent avatar to be display
                format: {
                  body: function (inner, coldex, rowdex) {
                    if (inner.length <= 0) return inner;
                    var el = $.parseHTML(inner);
                    var result = '';
                    $.each(el, function (index, item) {
                      if (item.classList !== undefined && item.classList.contains('user-name')) {
                        result = result + item.lastChild.firstChild.textContent;
                      } else if (item.innerText === undefined) {
                        result = result + item.textContent;
                      } else result = result + item.innerText;
                    });
                    return result;
                  }
                }
              }
            }
          ]
        },
        {
          text: '<i class="ti ti-plus me-0 me-sm-1 ti-xs"></i><span class="d-none d-sm-inline-block">Add New User</span>',
          className: 'add-new btn btn-primary waves-effect waves-light',
          attr: {
            'data-bs-toggle': 'offcanvas',
            'data-bs-target': '#offcanvasAddUser'
          }
        }
      ],
      // For responsive popup
      responsive: {
        details: {
          display: $.fn.dataTable.Responsive.display.modal({
            header: function (row) {
              var data = row.data();
              return 'Details of ' + data['full_name'];
            }
          }),
          type: 'column',
          renderer: function (api, rowIdx, columns) {
            var data = $.map(columns, function (col, i) {
              return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                ? '<tr data-dt-row="' +
                    col.rowIndex +
                    '" data-dt-column="' +
                    col.columnIndex +
                    '">' +
                    '<td>' +
                    col.title +
                    ':' +
                    '</td> ' +
                    '<td>' +
                    col.data +
                    '</td>' +
                    '</tr>'
                : '';
            }).join('');

            return data ? $('<table class="table"/><tbody />').append(data) : false;
          }
        }
      },
      initComplete: function () {
        // Adding role filter once table initialized
        this.api()
          .columns(3)
          .every(function () {
            var column = this;
            var select = $(
              '<select id="UserRole" class="form-select text-capitalize"><option value=""> Select Role </option></select>'
            )
              .appendTo('.user_role')
              .on('change', function () {
                var val = $.fn.dataTable.util.escapeRegex($(this).val());
                column.search(val ? '^' + val + '$' : '', true, false).draw();
              });

            column
              .data()
              .unique()
              .sort()
              .each(function (d, j) {
                select.append('<option value="' + d + '">' + d + '</option>');
              });
          });
        // Adding plan filter once table initialized
        this.api()
          .columns(4)
          .every(function () {
            var column = this;
            var select = $(
              '<select id="UserPlan" class="form-select text-capitalize"><option value=""> Select Plan </option></select>'
            )
              .appendTo('.user_plan')
              .on('change', function () {
                var val = $.fn.dataTable.util.escapeRegex($(this).val());
                column.search(val ? '^' + val + '$' : '', true, false).draw();
              });

            column
              .data()
              .unique()
              .sort()
              .each(function (d, j) {
                select.append('<option value="' + d + '">' + d + '</option>');
              });
          });
        // Adding status filter once table initialized
        this.api()
          .columns(6)
          .every(function () {
            var column = this;
            var select = $(
              '<select id="FilterTransaction" class="form-select text-capitalize"><option value=""> Select Status </option></select>'
            )
              .appendTo('.user_status')
              .on('change', function () {
                var val = $.fn.dataTable.util.escapeRegex($(this).val());
                column.search(val ? '^' + val + '$' : '', true, false).draw();
              });

            column
              .data()
              .unique()
              .sort()
              .each(function (d, j) {
                select.append(
                  '<option value="' +
                    statusObj[d].title +
                    '" class="text-capitalize">' +
                    statusObj[d].title +
                    '</option>'
                );
              });
          });
      }
    });
  }

  // Delete Record
  $('.datatables-users tbody').on('click', '.delete-record', function () {
    dt_user.row($(this).parents('tr')).remove().draw();
  });

  // Filter form control to default size
  // ? setTimeout used for multilingual table initialization
  setTimeout(() => {
    $('.dataTables_filter .form-control').removeClass('form-control-sm');
    $('.dataTables_length .form-select').removeClass('form-select-sm');
  }, 300);
});

// Validation & Phone mask
(function () {
  const phoneMaskList = document.querySelectorAll('.phone-mask'),
    addNewUserForm = document.getElementById('addNewUserForm');

  // Phone Number
  if (phoneMaskList) {
    phoneMaskList.forEach(function (phoneMask) {
      new Cleave(phoneMask, {
        phone: true,
        phoneRegionCode: 'LB'
      });
    });
  }
  // Add New User Form Validation
  const fv = FormValidation.formValidation(addNewUserForm, {
    fields: {
      userFullname: {
        validators: {
          notEmpty: {
            message: 'Please enter fullname '
          }
        }
      },
      userEmail: {
        validators: {
          notEmpty: {
            message: 'Please enter your email'
          },
          emailAddress: {
            message: 'The value is not a valid email address'
          }
        }
      }
    },
    plugins: {
      trigger: new FormValidation.plugins.Trigger(),
      bootstrap5: new FormValidation.plugins.Bootstrap5({
        // Use this for enabling/changing valid/invalid class
        eleValidClass: '',
        rowSelector: function (field, ele) {
          // field is the field name & ele is the field element
          return '.mb-6';
        }
      }),
      submitButton: new FormValidation.plugins.SubmitButton(),
      // Submit the form when all fields are valid
      // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
      autoFocus: new FormValidation.plugins.AutoFocus()
    }
  });
})();

