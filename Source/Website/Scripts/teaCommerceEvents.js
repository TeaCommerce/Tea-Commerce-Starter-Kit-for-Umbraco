var cartUpdateTimeout = null,
    cartTimeoutStart = null,
    cartMinimumTimeout = 1000;

TC.bind('afterCartUpdated', function (data, jQForm) {
  
  var cartTimeoutEnd = new Date().getTime(),
      timePast = cartTimeoutEnd - cartTimeoutStart,
      timeLeft = timePast > cartMinimumTimeout ? 0 : cartMinimumTimeout - timePast,
      miniCart = jQuery('#minicart');

  cartUpdateTimeout = window.setTimeout(function () {
    jQForm.add(miniCart).removeClass('loading');
    if (data.order) {
      miniCart.find('.quantity').text(data.order.totalQuantity);
      miniCart.find('.totalPrice').text(data.order.subtotalPrice.value.withVatFormatted);
    }
  }, timeLeft);
});

TC.bind('beforeAddOrUpdateOrderLine', function (data, jQForm) {
  jQForm.add(jQuery('#minicart')).addClass('loading');
  cartTimeoutStart = new Date().getTime();
  window.clearTimeout(cartUpdateTimeout);
});