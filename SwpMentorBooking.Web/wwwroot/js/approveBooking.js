$(document).ready(function () {
    $('#confirmModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        var bookingId = button.data('booking-id');
        $('#bookingIdInput').val(bookingId);
    });
    $('#completeModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        var bookingId = button.data('booking-id');
        $('#completeBookingIdInput').val(bookingId);
    });
    $('#cancelModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        var bookingId = button.data('booking-id');
        $('#cancelBookingIdInput').val(bookingId);
    });
});