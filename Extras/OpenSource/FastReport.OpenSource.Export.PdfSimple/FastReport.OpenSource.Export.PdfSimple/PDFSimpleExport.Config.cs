using FastReport.Utils;

namespace FastReport.Export.PdfSimple
{
    partial class PDFSimpleExport : ExportBase
    {
        #region Private Fields

        private string author;
        private int imageDpi = 300;
        private int jpegQuality = 90;
        private bool pdfA;
        private string keywords;
        private string subject;
        private string title;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Author of the document.
        /// </summary>
        public string Author
        {
            get { return author; }
            set { author = value; }
        }

        /// <summary>
        /// Gets or sets the fallback rasterization resolution used by Skia for PDF
        /// operations that cannot be represented as vector content. The default is
        /// 300 DPI; accepted values are clamped to the range 96-1200.
        /// </summary>
        public int ImageDpi
        {
            get { return imageDpi; }
            set
            {
                if (value > 1200)
                    value = 1200;
                else if (value < 96)
                    value = 96;
                imageDpi = value;
            }
        }

        /// <summary>
        /// Gets or sets JPEG quality for raster content. Values are clamped to 10-100.
        /// </summary>
        public int JpegQuality
        {
            get { return jpegQuality; }
            set
            {
                if (value > 100)
                    value = 100;
                else if (value < 10)
                    value = 10;
                jpegQuality = value;
            }
        }

        /// <summary>
        /// Gets or sets whether Skia should emit a PDF/A-2b compatible document.
        /// </summary>
        public bool PdfA
        {
            get { return pdfA; }
            set { pdfA = value; }
        }

        /// <summary>
        /// Keywords of the document.
        /// </summary>
        public string Keywords
        {
            get { return keywords; }
            set { keywords = value; }
        }

        /// <summary>
        /// Subject of the document.
        /// </summary>
        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }

        /// <summary>
        /// Title of the document.
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        #endregion Public Properties

        #region Public Methods

        /// <inheritdoc/>
        public override void Serialize(FRWriter writer)
        {
            base.Serialize(writer);

            writer.WriteInt("JpegQuality", JpegQuality);
            writer.WriteInt("ImageDpi", ImageDpi);
            writer.WriteBool("PdfA", PdfA);

            writer.WriteStr("Title", Title);
            writer.WriteStr("Author", Author);
            writer.WriteStr("Subject", Subject);
            writer.WriteStr("Keywords", Keywords);
        }

        #endregion Public Methods
    }
}
