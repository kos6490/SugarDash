using System;

namespace SugarDash.Presentation
{
    /// <summary>Bind가 돌려주는 해제 핸들. 한 번만 실행한다.</summary>
    internal sealed class Unbinder : IDisposable
    {
        private Action _unbind;

        public Unbinder(Action unbind) => _unbind = unbind;

        public void Dispose()
        {
            _unbind?.Invoke();
            _unbind = null;
        }
    }
}
