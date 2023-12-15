using System;

namespace AutoPolarAlign
{
    public interface IPolarAlignmentSolver : IDisposable
    {
        /// <summary>
        /// Altutide/Azimuth position where the mount is pointing relative to the pole
        /// </summary>
        Vec2 AlignmentOffset { get; }

        void Connect();

        void Disconnect();

        bool Solve(bool repeatUntilSuccess = true);
    }
}
