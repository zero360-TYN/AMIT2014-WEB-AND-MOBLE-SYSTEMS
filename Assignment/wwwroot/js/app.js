// import Swal from 'sweetalert2';

function userBlock(id) {
    Swal.fire({
        title: "Block User",
        icon: "warning",
        text: "Enter Block User Reason",
        input: "text",
        inputPlaceholder: "Enter Reason...",
        showCancelButton: true,
        cancelButtonText: "Cancel",
        confirmButtonText: "Block",

        inputValidator: function (value) {
            if (!value) {
                return "Please Enter Block Reason!";
            }
        }, // ErrorMessage popup, if there no any value in the input, the validation will appear

        customClass: {
            popup: "user-block-page", // the whole popup page
            title: "user-block-title",
            text: "user-block-text",
            icon: "user-block-icon",
            input: "user-block-input",
            confirmButton: "user-block-confirm-btn",
            cancelButton: "user-block-cancel-btn",
            // For validation css, swal2-validation-message
        }
    }).then(function (result) {
        if (result.isConfirmed) {
            var reason = result.value;

            window.location.href = "/Management/UserBlock?id=" + id + "&reason=" + encodeURIComponent(reason);
        }
    });
}

function userUnblock(id) {
    Swal.fire({
        title: "Unblock User",
        text: "Do you want to unblock user?",
        showCancelButton: true,
        showCloseButton: true,
        confirmButtonText: "Yes",
        cancelButtonText: "Cancel",

        customClass: {
            confirmButton: "user-unblock-confirm-btn",
            cancelButton: "user-unblock-cancel-btn",
        }
    }).then(function (result) {
        if (result.isConfirmed) {
            window.location.href = "/Management/UserUnblock?id=" + id;
        }
    });
}