using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace DrogueriaMickey.Desktop.Services
{
    // Envía el comando ESC/POS de apertura de cajón ("kick-out") directo
    // a la impresora térmica vía la cola de Windows. La caja registradora
    // no se conecta por su cuenta — recibe la señal desde la impresora
    // por el cable RJ11/RJ12 que las une físicamente.
    public class CashDrawerService
    {
        // ESC p 0 25 250 — comando estándar ESC/POS para activar el
        // solenoide del cajón (funciona en la gran mayoría de impresoras
        // térmicas compatibles, incluidas las genéricas tipo "POS-80").
        private static readonly byte[] OpenDrawerCommand = { 0x1B, 0x70, 0x00, 0x19, 0xFA };

        public void OpenDrawer(string? printerName = null)
        {
            var target = printerName ?? new PrinterSettings().PrinterName; // impresora predeterminada si no se especifica
            RawPrinterHelper.SendBytesToPrinter(target, OpenDrawerCommand);
        }
    }

    // Wrapper mínimo sobre la API Win32 para mandar bytes crudos a una
    // impresora, sin pasar por GDI+ (necesario porque el comando de
    // apertura de cajón es un comando de impresora, no una página impresa).
    internal static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            IntPtr hPrinter;
            var di = new DOCINFOA
            {
                pDocName = "Apertura de caja",
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                throw new InvalidOperationException($"No se pudo abrir la impresora '{printerName}'");

            try
            {
                if (!StartDocPrinter(hPrinter, 1, ref di))
                    throw new InvalidOperationException("No se pudo iniciar el documento de impresión");

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        throw new InvalidOperationException("No se pudo iniciar la página");

                    var unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);

                    try
                    {
                        bool success = WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out _);
                        EndPagePrinter(hPrinter);
                        return success;
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(unmanagedBytes);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}