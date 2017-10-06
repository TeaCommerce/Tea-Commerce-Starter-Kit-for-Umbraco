using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Services;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web;
using File = System.IO.File;

namespace TeaCommerce.StarterKit.Install {
  public class TeaCommerceStarterKitInstall : IPackageAction {
    public string Alias() {
      return "TeaCommerceStarterKitInstaller";
    }

    public bool Execute( string packageName, XmlNode xmlData ) {

      IMediaService mediaService = ApplicationContext.Current.Services.MediaService;
      IContentService contentService = UmbracoContext.Current.Application.Services.ContentService;
      IContentTypeService contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
      IFileService fileService = ApplicationContext.Current.Services.FileService;
      //ApplicationContext.Current.Services.DataTypeService.
      IDataTypeService dataTypeService = UmbracoContext.Current.Application.Services.DataTypeService;

      #region Create media
      List<int> productImageIds = new List<int>();
      List<IMedia> productImages = new List<IMedia>();
      try {
        //Create image files
        const string productImagesFolderName = "Product images";
        string[] mediaInstallImages = Directory.GetFiles( HostingEnvironment.MapPath( "~/installMedia" ) );
        IMedia productImagesFolder = mediaService.GetByLevel( 1 ).FirstOrDefault( m => m.Name == productImagesFolderName );
        if ( productImagesFolder == null ) {
          productImagesFolder = mediaService.CreateMedia( productImagesFolderName, -1, "Folder" );
          mediaService.Save( productImagesFolder );
        }

        if ( !productImagesFolder.Children().Any() ) {
          foreach ( string mediaInstallImage in mediaInstallImages ) {
            string fileName = Path.GetFileName( mediaInstallImage );
            IMedia productImage = mediaService.CreateMedia( fileName, productImagesFolder.Id, "Image" );
            byte[] buffer = File.ReadAllBytes( Path.GetFullPath( mediaInstallImage ) );
            using ( MemoryStream strm = new MemoryStream( buffer ) ) {

              productImage.SetValue( "umbracoFile", fileName, strm );
              mediaService.Save( productImage );
              productImages.Add( productImage );
              productImageIds.Add( productImage.Id );

            }
          }
        } else {
          productImageIds = productImagesFolder.Children().Select( c => c.Id ).ToList();
        }

        Directory.Delete( HostingEnvironment.MapPath( "~/installMedia" ), true );
      } catch ( Exception ex ) {
        LogHelper.Error<TeaCommerceStarterKitInstall>( "Create media failed", ex );
      }
      #endregion

      #region Set up Tea Commerce
      Store store = null;
      try {
        //Get store or create it
        IReadOnlyList<Store> stores = StoreService.Instance.GetAll().ToList();
        store = stores.FirstOrDefault();
        if ( store == null ) {
          store = new Store( "Starter Kit Store" );
          store.Save();
        }

        foreach ( PaymentMethod paymentMethod in PaymentMethodService.Instance.GetAll( store.Id ).Where( p => p.Alias == "invoicing" ) ) {
          PaymentMethodSetting setting = paymentMethod.Settings.FirstOrDefault( s => s.Key == "acceptUrl" );
          if ( setting != null ) {
            setting.Value = "/cart-content/confirmation/";
          }
          paymentMethod.Save();
        }
      } catch ( Exception ex ) {
        LogHelper.Error<TeaCommerceStarterKitInstall>( "Setting up Tea Commerce failed", ex );
      }
      #endregion

      #region Create Templates
      List<ITemplate> templates = new List<ITemplate>();
      try {
        string[] templateFiles = Directory.GetFiles( HostingEnvironment.MapPath( "~/views" ) );
        foreach ( string templateFile in templateFiles ) {
          string fileName = Path.GetFileNameWithoutExtension( templateFile );
          string fileContent = File.ReadAllText( templateFile );
          templates.Add( fileService.CreateTemplateWithIdentity( fileName, fileContent ) );
        }
      } catch ( Exception ex ) {
        LogHelper.Error<TeaCommerceStarterKitInstall>( "Create templates failed", ex );
      }
      #endregion

      #region Set up content types

      IEnumerable<IDataTypeDefinition> allDataTypeDefinitions = dataTypeService.GetAllDataTypeDefinitions();
      try {
        IDataTypeDefinition variantEditorDataTypeDefinition = allDataTypeDefinitions.FirstOrDefault( d => d.Name.ToLowerInvariant().Contains( "variant editor" ) );
        variantEditorDataTypeDefinition.DatabaseType = DataTypeDatabaseType.Ntext;

        //dataTypeService.SavePreValues( variantEditorDataTypeDefinition, new Dictionary<string, PreValue> {
        //  { "xpathOrNode", new PreValue("{\"showXPath\": true,\"query\": \"$current/ancestor-or-self::Frontpage/attributes\"}") },
        //  { "variantDocumentType", new PreValue("Variant") },
        //  { "extraListInformation", new PreValue("sku,priceJMD") },
        //  { "hideLabel", new PreValue("1") },

        //} );
        var preValDictionary = new Dictionary<string, object> {
          { "xpathOrNode", "{\"showXPath\": true,\"query\": \"$current/ancestor-or-self::Frontpage/attributes\"}" },
          { "variantDocumentType", "Variant" },
          { "extraListInformation", "sku,priceJMD" },
          { "hideLabel", "1" },

        };
        var currVal = dataTypeService.GetPreValuesCollectionByDataTypeId( variantEditorDataTypeDefinition.Id );

        //we need to allow for the property editor to deserialize the prevalues
        PropertyEditor pe = PropertyEditorResolver.Current.PropertyEditors.SingleOrDefault( x => x.Alias == "TeaCommerce.VariantEditor" );
        var formattedVal = pe.PreValueEditor.ConvertEditorToDb( preValDictionary, currVal );
        dataTypeService.SaveDataTypeAndPreValues( variantEditorDataTypeDefinition, formattedVal );
        //dataTypeService.Save( variantEditorDataTypeDefinition );
        variantEditorDataTypeDefinition = dataTypeService.GetDataTypeDefinitionById( variantEditorDataTypeDefinition.Id );
      } catch ( Exception ex ) {
        LogHelper.Error<TeaCommerceStarterKitInstall>( "Set up content types failed", ex );
      }
      #endregion

      #region Create Document types
      ContentType attributeContentType = null,
        attributeGroupContentType = null,
        attributesContentType = null,
        cartStepContentType = null,
        cartContentType = null,
        variantContentType = null,
        productContentType = null,
        productListContentType = null,
        frontpageContentType = null;
      try {

        attributeContentType = new ContentType( -1 );
        attributeContentType.Alias = "attribute";
        attributeContentType.Name = "Attribute";
        attributeContentType.Icon = "icon-t-shirt";
        contentTypeService.Save( attributeContentType );

        attributeGroupContentType = new ContentType( -1 );
        attributeGroupContentType.Alias = "attributeGroup";
        attributeGroupContentType.Name = "Attribute group";
        attributeGroupContentType.Icon = "icon-t-shirt color-orange";
        attributeGroupContentType.AllowedContentTypes = new List<ContentTypeSort>() { new ContentTypeSort( attributeContentType.Id, 0 ) };
        contentTypeService.Save( attributeGroupContentType );

        attributesContentType = new ContentType( -1 );
        attributesContentType.Alias = "attributes";
        attributesContentType.Name = "Attributes";
        attributesContentType.Icon = "icon-t-shirt";
        attributesContentType.AllowedContentTypes = new List<ContentTypeSort>() { new ContentTypeSort( attributeGroupContentType.Id, 0 ) };
        contentTypeService.Save( attributesContentType );

        cartStepContentType = new ContentType( -1 );
        cartStepContentType.Alias = "cartStep";
        cartStepContentType.Name = "Cart step";
        cartStepContentType.Icon = "icon-shopping-basket-alt-2 color-orange";
        cartStepContentType.AllowedTemplates = templates.Where( t => t.Alias.ToLowerInvariant().Contains( "cartstep" ) && !t.Alias.ToLowerInvariant().Contains( "master" ) );
        contentTypeService.Save( cartStepContentType );

        cartContentType = new ContentType( -1 );
        cartContentType.Alias = "cart";
        cartContentType.Name = "Cart";
        cartContentType.Icon = "icon-shopping-basket-alt-2";
        cartContentType.AllowedContentTypes = new List<ContentTypeSort>() { new ContentTypeSort( cartStepContentType.Id, 0 ) };
        cartContentType.AllowedTemplates = templates.Where( t => t.Alias.ToLowerInvariant().Contains( "cartstep1" ) );
        contentTypeService.Save( cartContentType );

        variantContentType = CreateVariantContentType( allDataTypeDefinitions, contentTypeService );
        productContentType = CreateProductContentType( allDataTypeDefinitions, contentTypeService, templates );

        productListContentType = new ContentType( -1 );
        productListContentType.Alias = "productList";
        productListContentType.Name = "Product list";
        productListContentType.Icon = "icon-tags";
        productListContentType.AllowedContentTypes = new List<ContentTypeSort>() { new ContentTypeSort( productContentType.Id, 0 ) };
        productListContentType.AllowedTemplates = templates.Where( t => t.Alias.ToLowerInvariant().Contains( "productlist" ) );
        contentTypeService.Save( productListContentType );

        frontpageContentType = CreateFrontpageContentType( allDataTypeDefinitions, contentTypeService, templates );
      } catch ( Exception ex ) {
        LogHelper.Error<TeaCommerceStarterKitInstall>( "Create templates failed", ex );
      }
      #endregion

      #region Create content
      try {
        Content frontPageContent = new Content( "Home", -1, frontpageContentType );
        frontPageContent.Template = frontpageContentType.AllowedTemplates.First();
        frontPageContent.SetValue( "slider", string.Join( ",", productImages.Select( p => "umb://media/" + p.Key.ToString().Replace( "-", "" ) ) ) );
        frontPageContent.SetValue( "store", store.Id );
        contentService.SaveAndPublishWithStatus( frontPageContent, raiseEvents: false );

        #region Create Cart
        Content cartContent = new Content( "Cart content", frontPageContent.Id, cartContentType );
        cartContent.Template = cartContentType.AllowedTemplates.First();
        contentService.SaveAndPublishWithStatus( cartContent, raiseEvents: false );
        Content informationContent = new Content( "Information", cartContent.Id, cartStepContentType );
        informationContent.Template = templates.First( t => t.Alias.ToLowerInvariant() == "cartstep2" );
        contentService.SaveAndPublishWithStatus( informationContent, raiseEvents: false );
        Content shippingPaymentContent = new Content( "Shipping/Payment", cartContent.Id, cartStepContentType );
        shippingPaymentContent.Template = templates.First( t => t.Alias.ToLowerInvariant() == "cartstep3" );
        contentService.SaveAndPublishWithStatus( shippingPaymentContent, raiseEvents: false );
        Content acceptContent = new Content( "Accept", cartContent.Id, cartStepContentType );
        acceptContent.Template = templates.First( t => t.Alias.ToLowerInvariant() == "cartstep4" );
        contentService.SaveAndPublishWithStatus( acceptContent, raiseEvents: false );
        Content paymentContent = new Content( "Payment", cartContent.Id, cartStepContentType );
        contentService.SaveAndPublishWithStatus( paymentContent );
        Content confirmationContent = new Content( "Confirmation", cartContent.Id, cartStepContentType );
        confirmationContent.Template = templates.First( t => t.Alias.ToLowerInvariant() == "cartstep6" );
        contentService.SaveAndPublishWithStatus( confirmationContent, raiseEvents: false );
        #endregion

        #region Create Attributes
        Content variantAttributesContent = new Content( "Variant attributes", frontPageContent.Id, attributesContentType );
        contentService.SaveAndPublishWithStatus( variantAttributesContent, raiseEvents: false );
        Content colorContent = new Content( "Color", variantAttributesContent.Id, attributeGroupContentType );
        contentService.SaveAndPublishWithStatus( colorContent, raiseEvents: false );
        Content sizeContent = new Content( "Size", variantAttributesContent.Id, attributeGroupContentType );
        contentService.SaveAndPublishWithStatus( sizeContent, raiseEvents: false );
        Content blackContent = new Content( "Black", colorContent.Id, attributeContentType );
        contentService.SaveAndPublishWithStatus( blackContent, raiseEvents: false );
        Content whiteContent = new Content( "White", colorContent.Id, attributeContentType );
        contentService.SaveAndPublishWithStatus( whiteContent, raiseEvents: false );
        Content blueContent = new Content( "Blue", colorContent.Id, attributeContentType );
        contentService.SaveAndPublishWithStatus( blueContent, raiseEvents: false );
        Content largeContent = new Content( "Large", sizeContent.Id, attributeContentType );
        contentService.SaveAndPublishWithStatus( largeContent, raiseEvents: false );
        Content mediumContent = new Content( "Medium", sizeContent.Id, attributeContentType );
        contentService.SaveAndPublishWithStatus( mediumContent, raiseEvents: false );
        Content smallContent = new Content( "Small", sizeContent.Id, attributeContentType );
        contentService.SaveAndPublishWithStatus( smallContent, raiseEvents: false );
        #endregion

        #region Create Products
        Content expensiveYachtsContent = new Content( "Expensive Yachts", frontPageContent.Id, productListContentType );
        expensiveYachtsContent.Template = productListContentType.AllowedTemplates.First();
        contentService.SaveAndPublishWithStatus( expensiveYachtsContent, raiseEvents: false );
        Content veryAxpensiveYachtsContent = new Content( "Very expensive Yachts", frontPageContent.Id, productListContentType );
        veryAxpensiveYachtsContent.Template = productListContentType.AllowedTemplates.First();
        contentService.SaveAndPublishWithStatus( veryAxpensiveYachtsContent, raiseEvents: false );
        Content summerYachtContent = new Content( "Summer Yacht", expensiveYachtsContent, productContentType );
        summerYachtContent.Template = productContentType.AllowedTemplates.First();
        summerYachtContent.SetValue( "image", "umb://media/" + productImages[0].Key.ToString().Replace( "-", "" ) );
        summerYachtContent.SetValue( "productName", "Summer Yacht" );
        summerYachtContent.SetValue( "sku", "p0001" );
        summerYachtContent.SetValue( "priceJMD", "500" );
        summerYachtContent.SetValue( "description", "<p>This is the product description.</p>" );
        summerYachtContent.SetValue( "variants", "{\"variants\": [],\"variantGroupsOpen\":{}}" );
        contentService.SaveAndPublishWithStatus( summerYachtContent, raiseEvents: false );
        Content yachtWithSailsContent = new Content( "Yacht with sails", expensiveYachtsContent, productContentType );
        yachtWithSailsContent.Template = productContentType.AllowedTemplates.First();
        yachtWithSailsContent.SetValue( "image", "umb://media/" + productImages[1].Key.ToString().Replace( "-", "" ) );
        yachtWithSailsContent.SetValue( "productName", "Yacht with sails" );
        yachtWithSailsContent.SetValue( "sku", "p0002" );
        yachtWithSailsContent.SetValue( "priceJMD", "1000" );
        yachtWithSailsContent.SetValue( "description", "<p>This is the product description.</p>" );
        yachtWithSailsContent.SetValue( "variants", "{\"variants\": [],\"variantGroupsOpen\":{}}" );
        contentService.SaveAndPublishWithStatus( yachtWithSailsContent, raiseEvents: false );
        Content motorDrivenYachtContent = new Content( "Motor driven Yacht", veryAxpensiveYachtsContent, productContentType );
        motorDrivenYachtContent.Template = productContentType.AllowedTemplates.First();
        motorDrivenYachtContent.SetValue( "image", "umb://media/" + productImages[2].Key.ToString().Replace( "-", "" ) );
        motorDrivenYachtContent.SetValue( "productName", "Motor driven Yacht" );
        motorDrivenYachtContent.SetValue( "sku", "p0003" );
        motorDrivenYachtContent.SetValue( "priceJMD", "1500" );
        motorDrivenYachtContent.SetValue( "description", "<p>This is the product description.</p>" );
        motorDrivenYachtContent.SetValue( "variants", "{\"variants\": [],\"variantGroupsOpen\":{}}" );
        contentService.SaveAndPublishWithStatus( motorDrivenYachtContent, raiseEvents: false );
        Content oneMastedYachtContent = new Content( "One masted yacht", veryAxpensiveYachtsContent, productContentType );
        oneMastedYachtContent.Template = productContentType.AllowedTemplates.First();
        oneMastedYachtContent.SetValue( "image", "umb://media/" + productImages[3].Key.ToString().Replace( "-", "" ) );
        oneMastedYachtContent.SetValue( "productName", "One masted yacht" );
        oneMastedYachtContent.SetValue( "sku", "p0004" );
        oneMastedYachtContent.SetValue( "priceJMD", "2000" );
        oneMastedYachtContent.SetValue( "description", "<p>This is the product description.</p>" );
        oneMastedYachtContent.SetValue( "variants", "{\"variants\": [],\"variantGroupsOpen\":{}}" );


        contentService.SaveAndPublishWithStatus( oneMastedYachtContent, raiseEvents: false );


        #endregion
        frontPageContent.SetValue( "featuredProducts", string.Join( ",", new List<string> {
          "umb://document/"+summerYachtContent.Key.ToString().Replace("-",""),
          "umb://document/"+yachtWithSailsContent.Key.ToString().Replace("-",""),
          "umb://document/"+motorDrivenYachtContent.Key.ToString().Replace("-",""),
          "umb://document/"+oneMastedYachtContent.Key.ToString().Replace("-",""),
        } ) );
        contentService.SaveAndPublishWithStatus( frontPageContent, raiseEvents: false );

      } catch ( Exception ex ) {
        LogHelper.Error<TeaCommerceStarterKitInstall>( "Create content failed", ex );
      }

      #endregion


      return true;

    }

    public XmlNode SampleXml() {
      return helper.parseStringToXmlNode( string.Format( @"<Action runat=""install"" alias=""{0}"" />", Alias() ) );
    }

    public bool Undo( string packageName, XmlNode xmlData ) {
      //Remove stuff
      return true;
    }

    private ContentType CreateVariantContentType( IEnumerable<IDataTypeDefinition> allDataTypeDefinitions, IContentTypeService contentTypeService ) {
      ContentType contentType = new ContentType( -1 );
      contentType.Alias = "variant";
      contentType.Name = "Variant";
      contentType.Icon = "icon-folder";

      PropertyGroup contentPropertyGroup = new PropertyGroup( new PropertyTypeCollection( new List<PropertyType> {
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "TeaCommerce.StockManagement") ) {
          Alias = "stock",
          Name = "Stock",
          Description = "Remember to add a sku for the product. Without a sku the variant cannot have it's own stock.",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.Textbox") ) {
          Alias = "productName",
          Name = "Name",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.Textbox") ) {
          Alias = "sku",
          Name = "Sku",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.Textbox") ) {
          Alias = "priceJMD",
          Name = "Price",
          ValidationRegExp = @"^(\s*|\d+(?:[,|\.]\d+)?)$",
        },
      } ) ) {
        Name = "Content"
      };
      contentType.PropertyGroups.Add( contentPropertyGroup );
      contentTypeService.Save( contentType );

      return contentType;
    }

    private ContentType CreateProductContentType( IEnumerable<IDataTypeDefinition> allDataTypeDefinitions, IContentTypeService contentTypeService, List<ITemplate> templates ) {
      ContentType contentType = new ContentType( -1 );
      contentType.Alias = "product";
      contentType.Name = "Product";
      contentType.Icon = "icon-tag";
      contentType.AllowedTemplates = templates.Where( t => t.Alias.ToLowerInvariant() == "product" );

      PropertyGroup contentPropertyGroup = new PropertyGroup( new PropertyTypeCollection( new List<PropertyType> {
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.MediaPicker2" && !d.Name.ToLowerInvariant().Contains("multiple")) ) {
          Alias = "image",
          Name = "Image",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.Textbox") ) {
          Alias = "productName",
          Name = "Name",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.Textbox") ) {
          Alias = "sku",
          Name = "Sku",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.Textbox") ) {
          Alias = "priceJMD",
          Name = "Price",
          ValidationRegExp = @"^(\s*|\d+(?:[,|\.]\d+)?)$",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "TeaCommerce.StockManagement") ) {
          Alias = "stock",
          Name = "Stock",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.TinyMCEv3") ) {
          Alias = "description",
          Name = "Description",
        },
      } ) ) {
        Name = "Content"
      };
      contentType.PropertyGroups.Add( contentPropertyGroup );

      PropertyGroup variantsPropertyGroup = new PropertyGroup( new PropertyTypeCollection( new List<PropertyType> {
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "TeaCommerce.VariantEditor") ) {
          Alias = "variants",
          Name = "Variants",
        },
      } ) ) {
        Name = "Variants"
      };
      contentType.PropertyGroups.Add( variantsPropertyGroup );
      contentTypeService.Save( contentType );

      return contentType;
    }

    private ContentType CreateFrontpageContentType( IEnumerable<IDataTypeDefinition> allDataTypeDefinitions, IContentTypeService contentTypeService, List<ITemplate> templates ) {
      ContentType contentType = new ContentType( -1 );
      contentType.Alias = "frontpage";
      contentType.Name = "Frontpage";
      contentType.Icon = "icon-home";
      contentType.AllowedAsRoot = true;
      contentType.AllowedTemplates = templates.Where( t => t.Alias.ToLowerInvariant().Contains( "frontpage" ) );

      PropertyGroup contentPropertyGroup = new PropertyGroup( new PropertyTypeCollection( new List<PropertyType> {
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.MediaPicker2" && d.Name.ToLowerInvariant().Contains("multiple")) ) {
          Alias = "slider",
          Name = "Slider",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "TeaCommerce.StorePicker") ) {
          Alias = "store",
          Name = "Store",
        },
        new PropertyType( allDataTypeDefinitions.FirstOrDefault(d => d.PropertyEditorAlias == "Umbraco.MultiNodeTreePicker2" && d.Name.ToLowerInvariant().Contains("product picker")) ) {
          Alias = "featuredProducts",
          Name = "Featured products",
        },
      } ) ) {
        Name = "Content"
      };
      contentType.PropertyGroups.Add( contentPropertyGroup );
      contentTypeService.Save( contentType );

      return contentType;
    }

  }
}
