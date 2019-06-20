/**********************************
  * PRODUCT
***********************************/
jQuery(function () {

  if (window['_products']) {

    function variantChange(event) {
      var select = jQuery(this),
          form = select.closest('form'),
          productId = parseInt(form.attr('data-product-id')),
          attributeGroups = form.find('select[data-attribute-group]'),
          productIdentifierField = form.find('[name=productIdentifier]'),
          submitButtons = form.find('[type="submit"]'),
          reachedEnd = false,
          reachedInitiator = false,
          filters = [],
          initiator = event.target;

      if (productId) {
        var filteredVariants = jQuery.extend({}, _products[productId]); //Avoid keeping a reference to the original variants object

        //Reset select status
        attributeGroups.each(function() {
          var attributeGroup = jQuery(this);

          if (reachedInitiator) {
            attributeGroup.val('');
          }

          //Hide and reset all elements after an empty select
          if (reachedEnd) {
            attributeGroup.prop('disabled', true);
            attributeGroup.val('');
          } else {
            attributeGroup.prop('disabled', false);

            if (attributeGroup.val()) {
              filters.push(attributeGroup.val());
            }
          }

          if (attributeGroup.val() == '') {
            reachedEnd = true;
          }

          if (attributeGroup.is(initiator)) {
            reachedInitiator = true;
          }
        });

        //Find all potential variants with the given filters
        jQuery.each(filteredVariants, function(id, variant) {
          for (var i = 0; i < filters.length; i++) {
            var filter = filters[i];

            if (jQuery.inArray(filter, variant.combinations) == -1) {
              delete filteredVariants[id];
            }
          }
        });

        //Disable options with no variants
        attributeGroups.each(function() {
          var attributeGroup = jQuery(this),
            options = attributeGroup.find('option');

          if (attributeGroup.val() == '') {
            options.each(function() {
              var option = jQuery(this);

              if (option.val()) {
                option.prop('disabled', true);

                jQuery.each(filteredVariants, function(id, variant) {
                  if (jQuery.inArray(option.val(), variant.combinations) > -1) {

                    option.prop('disabled', false);

                    return false;
                  }
                });
              }
            });
          }
        });

        if (filteredVariants && Object.keys(filteredVariants).length === 1) {
          //One single variant is found
          var variantId = getFirstKey(filteredVariants),
              variant = filteredVariants[variantId],
              productIdentifier = variant.productIdentifier;
          productIdentifierField.val(productIdentifier);
          submitButtons.removeClass('disabled');
          //INSERT CODE HERE

        } else {
          productIdentifierField.val('');
          submitButtons.addClass('disabled');
        }
      }
    }

    function initVariants() {
      jQuery('form[action="/base/TC/FormPost.aspx"]').each(function() {
        var form = jQuery(this),
            attributeGroups = form.find('select[data-attribute-group]');
        if (attributeGroups[0]) {
          attributeGroups.not(':first').prop('disabled', true);

          attributeGroups.on('change', variantChange);
        } else {
          form.find('input.btn').removeClass('disabled');
        }
      });

    }

    function getFirstKey(data) {
      for (var prop in data)
        if (data.propertyIsEnumerable(prop))
          return prop;
    }

    initVariants();
  }
});