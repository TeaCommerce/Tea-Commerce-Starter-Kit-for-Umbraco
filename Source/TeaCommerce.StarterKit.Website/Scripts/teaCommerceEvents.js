/**********************************
  * TEA COMMERCE EVENTS
***********************************/
//Global variables
var cartUpdateTimeout = null,
    cartTimeoutStart = null,
    cartMinimumTimeout = 1000; //The minimum time to show the user a change is happening. Otherwise he might not get to see it.

//When an orderline is being updated
TC.bind('beforeAddOrUpdateOrderLine', function (data, jQForm) {
  //Start loading the minicart
  jQuery('#minicart').addClass('loading');
  if (jQForm && jQForm[0]) {
    jQForm.addClass('loading');
  }
  //Set start time for the change event
  cartTimeoutStart = new Date().getTime();
  //Clear any old cart update timeouts
  window.clearTimeout(cartUpdateTimeout);
});

//After the cart have been updated
TC.bind('afterCartUpdated', function (data, jQForm) {

  var cartTimeoutEnd = new Date().getTime(), //Set end time for the change event
      timePast = cartTimeoutEnd - cartTimeoutStart,//Calculate the time since start
      timeLeft = timePast > cartMinimumTimeout ? 0 : cartMinimumTimeout - timePast, //Calculate the time left to show loading status
      miniCart = jQuery('#minicart');

  //Set a timeout for the remainder of the time
  cartUpdateTimeout = window.setTimeout(function () {
    //Remove loading status from the mini cart
    miniCart.removeClass('loading');
    if (jQForm && jQForm[0]) {
      //Remove loading status from the form if there is one
      jQForm.removeClass('loading');
    }
    //If an order have been returned we need to update the UI
    if (data.order) {
      //Update quantity
      miniCart.find('.quantity').text(data.order.totalQuantity);
      //Update the total price
      miniCart.find('.totalPrice').text(data.order.subtotalPrice.value.withVatFormatted);

      //Update css classes on the minicart depending on the order status
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