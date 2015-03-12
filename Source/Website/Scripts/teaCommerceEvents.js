var cartUpdateTimeout = null,
    cartTimeoutStart = null,
    cartMinimumTimeout = 1000;

TC.bind('afterCartUpdated', function (data, jQForm) {

  var cartTimeoutEnd = new Date().getTime(),
      timePast = cartTimeoutEnd - cartTimeoutStart,
      timeLeft = timePast > cartMinimumTimeout ? 0 : cartMinimumTimeout - timePast,
      miniCart = jQuery('#minicart');

  cartUpdateTimeout = window.setTimeout(function () {
    miniCart.removeClass('loading');
    if (jQForm && jQForm[0]) {
      jQForm.removeClass('loading');
    }
    if (data.order) {
      miniCart.find('.quantity').text(data.order.totalQuantity);
      miniCart.find('.totalPrice').text(data.order.subtotalPrice.value.withVatFormatted);
      miniCart.removeClass('oneItem empty');
      if (data.order.totalQuantity > 0) {
        miniCart.find('a').removeClass('disabled');
        if (data.order.totalQuantity === 1) {
          miniCart.addClass('oneItem');
        }
      } else if (data.order.totalQuantity === 0) {
        miniCart.addClass('empty');
        miniCart.find('a').addClass('disabled');
      }
    }
  }, timeLeft);
});

TC.bind('beforeAddOrUpdateOrderLine', function (data, jQForm) {
  jQForm.add(jQuery('#minicart')).addClass('loading');
  cartTimeoutStart = new Date().getTime();
  window.clearTimeout(cartUpdateTimeout);
});