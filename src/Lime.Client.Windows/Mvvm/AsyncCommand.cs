using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Lime.Client.Windows.Mvvm
{
    /// <summary>
    /// http://blog.mycupof.net/2012/08/23/mvvm-asyncdelegatecommand-what-asyncawait-can-do-for-uidevelopment/
    /// </summary>    
    public class AsyncCommand : ICommand
    {
        #region Fields

        protected readonly Func<object, bool> _canExecute;
        protected Func<object, Task> _asyncExecute;

        #endregion

        #region Constructor

        public AsyncCommand(Func<object, Task> execute)
            : this(execute, null)
        {


        }

        public AsyncCommand(Func<object, Task> asyncExecute, Func<object, bool> canExecute)
        {
            _asyncExecute = asyncExecute;
            _canExecute = canExecute;
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;


        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter).ConfigureAwait(false);
        }

        #endregion

        protected virtual async Task ExecuteAsync(object parameter)
        {
            await _asyncExecute(parameter).ConfigureAwait(false);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged.RaiseEvent(this, EventArgs.Empty);
        }
    }
}
