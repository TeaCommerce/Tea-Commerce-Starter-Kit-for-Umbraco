using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Services;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using ContentExtensions = umbraco.ContentExtensions;
using File = System.IO.File;

namespace TeaCommerce.StarterKit.Install {
  public class TeaCommerceStarterKitInstall : IPackageAction {
    public string Alias() {
      return "TeaCommerceStarterKitInstaller";
    }

    public bool Execute( string packageName, XmlNode xmlData ) {
      IMediaService mediaService = ApplicationContext.Current.Services.MediaService;
      IContentService contentService = UmbracoContext.Current.Application.Services.ContentService;

      //Create image files
      const string productImagesFolderName = "Product images";
      List<int> productImageIds = new List<int>();
      string[] mediaInstallImages = Directory.GetFiles( HttpContext.Current.Server.MapPath( "~/installMedia" ) );
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
      foreach ( string mediaInstallImage in mediaInstallImages ) {
        File.Delete( mediaInstallImage );
      }
      Directory.Delete( HttpContext.Current.Server.MapPath( "~/installMedia" ) );

      //Get store
      IReadOnlyList<Store> stores = StoreService.Instance.GetAll().ToList();
      Store store = stores.FirstOrDefault();
      if ( store == null ) {
        store = new Store( "Starter Kit Store" );
        store.Save();
      }

      //Update languages and products
      IReadOnlyList<IContent> langContents = contentService.GetByLevel( 1 ).Where( c => c.ContentType.Alias == "Lang" && !c.Published && c.CreateDate > DateTime.Now.AddMinutes( -5 ) ).ToList();
      if ( langContents.Any() ) {
        foreach ( IContent langContent in langContents ) {
          IReadOnlyList<IContent> products = langContent.Descendants().Where( c => c.ContentType.Alias == "Product" ).ToList();
          langContent.SetValue( "featuredProducts", string.Join( ",", products.Take( 4 ).Select( c => c.Id ) ) );
          langContent.SetValue( "slider", string.Join( ",", productImageIds ) );
          langContent.SetValue( "store", store.Id );
          contentService.Save( langContent );

          int count = 0;
          foreach ( IContent productContent in products ) {
            int mediaId = productImageIds[ count ];
            productContent.SetValue( "image", mediaId );
            contentService.Save( productContent );
            count++;
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
