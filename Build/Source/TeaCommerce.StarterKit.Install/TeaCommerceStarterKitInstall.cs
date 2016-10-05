using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Xml;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Services;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;
using Umbraco.Core;
using Umbraco.Core.Models;
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

      //Create image files
      const string productImagesFolderName = "Product images";
      List<int> productImageIds = new List<int>();
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
            productImageIds.Add( productImage.Id );

          }
        }
      } else {
        productImageIds = productImagesFolder.Children().Select( c => c.Id ).ToList();
      }

      Directory.Delete( HostingEnvironment.MapPath( "~/installMedia" ), true );

      //Get store or create it
      IReadOnlyList<Store> stores = StoreService.Instance.GetAll().ToList();
      Store store = stores.FirstOrDefault();
      if ( store == null ) {
        store = new Store( "Starter Kit Store" );
        store.Save();
      }

      //Update languages and products
      IReadOnlyList<IContent> homeContents = contentService.GetByLevel( 1 ).Where( c => c.ContentType.Alias == "Home" && !c.Published && c.CreateDate > DateTime.Now.AddMinutes( -5 ) ).ToList();
      if ( homeContents.Any() ) {
        foreach ( IContent homeContent in homeContents ) {
          IReadOnlyList<IContent> products = homeContent.Descendants().Where( c => c.ContentType.Alias == "Product" ).ToList();
          homeContent.SetValue( "featuredProducts", string.Join( ",", products.Take( 4 ).Select( c => c.Id ) ) );
          homeContent.SetValue( "slider", string.Join( ",", productImageIds ) );
          homeContent.SetValue( "store", store.Id );
          contentService.Save( homeContent );

          //Set image on product
          int count = 0;
          foreach ( IContent productContent in products ) {
            int mediaId = productImageIds[ count ];
            productContent.SetValue( "image", mediaId );
            contentService.Save( productContent );
            count++;
          }

          //Fix Cart step templates
          IReadOnlyList<IContent> cartSteps = homeContent.Descendants().Where( c => c.ContentType.Alias == "CartStep" ).OrderBy( c => c.SortOrder ).ToList();
          if ( cartSteps.Any() ) {

            IContentType contentType = contentTypeService.GetContentType( cartSteps.First().ContentTypeId );
            IEnumerable<ITemplate> templates = contentType.AllowedTemplates;
            count = 2;
            foreach ( IContent cartStep in cartSteps ) {
              string templateAlias = "CartStep" + count;
              ITemplate template = templates.FirstOrDefault( t => t.Alias == templateAlias );
              if ( template != null ) {
                cartStep.Template = template;
                contentService.Save( cartStep );
              }
              count++;
            }
          }
        }
      }



      return true;

    }

    public XmlNode SampleXml() {
      return helper.parseStringToXmlNode( string.Format( @"<Action runat=""install"" alias=""{0}"" />", Alias() ) );
    }

    public bool Undo( string packageName, XmlNode xmlData ) {
      //Remove stuff
      return true;
    }
  }
}
