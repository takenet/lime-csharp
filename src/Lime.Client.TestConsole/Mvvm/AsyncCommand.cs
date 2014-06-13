using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Lime.Client.TestConsole.Mvvm
{
    /// <summary>
    /// http://blog.mycupof.net/2012/08/23/mvvm-asyncdelegatecommand-what-asyncawait-can-do-for-uidevelopment/
    /// </summary>    
    public class AsyncCommand : ICommand
    {
        #region Fields

        protected readonly Func<object, bool> _canExecuteFunc;
        protected readonly Func<object, Task> _executeFunc;

        #endregion

        #region Constructor

        public AsyncCommand(Func<Task> executeFunc)
            : this(p => executeFunc())
        {

        }

        public AsyncCommand(Func<object, Task> executeFunc)
            : this(executeFunc, () => true)
        {

        }

        public AsyncCommand(Func<Task> executeFunc, Func<bool> canExecuteFunc)
            : this(p => executeFunc(), p => canExecuteFunc())
        {

        }

        public AsyncCommand(Func<object, Task> executeFunc, Func<bool> canExecuteFunc)
            : this(executeFunc, p => canExecuteFunc())
        {
            
        }

        public AsyncCommand(Func<object, Task> executeFunc, Func<object, bool> canExecuteFunc)           
        {
            _executeFunc = executeFunc;
            _canExecuteFunc = canExecuteFunc;
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
            return _canExecuteFunc(parameter);
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

        public virtual Task ExecuteAsync(object parameter)
        {
            return _executeFunc(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged.RaiseEvent(this, EventArgs.Empty);
        }
    }
}
