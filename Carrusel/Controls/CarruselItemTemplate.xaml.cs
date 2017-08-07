using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Carrusel.Controls
{
    public sealed partial class CarruselItemTemplate : UserControl
    {
        public DropShadow Shadow
        {
            get
            {
                return shadow.DropShadow;
            }
        }

        public Visual OverlayPanel
        {
            get
            {
                return ElementCompositionPreview.GetElementVisual(pnlDarkOverlay);
            }
        }

        public CarruselItemTemplate()
        {
            this.InitializeComponent();
        }

        public Visual GetVisual()
        {
            return ElementCompositionPreview.GetElementVisual(pnlRoot);
        }
    }
}
