using System;

namespace AutoPolarAlign
{
    public interface IPolarAlignmentMount : IDisposable
    {
        void Connect();

        void Disconnect();

        void MoveAltitude(double amount);

        void MoveAzimuth(double amount);

        void StopAltitude();

        void StopAzimuth();
    }
}
