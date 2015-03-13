/**********************************
  * PRODUCT
***********************************/
jQuery(function () {
  //Get variant on page
  var variants = jQuery('#variants');
  if (variants[0]) {
    
    //When variants are selected by the user we change the UI
    variants.change(function() {
      var selectedVariant = jQuery(this).val();
      //Hide all variant information
      jQuery('[data-variantid]').addClass('hidden');
      //Show variant information for the selected variant
      jQuery('[data-variantid=' + selectedVariant + ']').removeClass('hidden');
    });
  }
});