using System;
using System.Collections.Generic;

namespace Tool.Shared.Framework
{
    public class ViewModelPageBind
    {
        public ViewModelPageBind()
        {
        }

        public ViewModelPageBind(IEnumerable<(string name, Type page, Func<IViewModel> createViewModel)> list)
        {
            RegisterPage(list);
        }

        public void RegisterPage(IEnumerable<(string name, Type page, Func<IViewModel> createViewModel)> list)
        {
            foreach (var (name, page, createViewModel) in list)
            {
                RegisterPage(name, page, createViewModel);
            }
        }

        private void RegisterPage(string name, Type page, Func<IViewModel> createViewModel)
        {
            ViewModelPageList.Add(name, (page, createViewModel));
        }

        // string name, Type page, Lazy<IViewModel> createViewModel
        private Dictionary<string, (Type page, Func<IViewModel> createViewModel)> ViewModelPageList { get; } = new Dictionary<string, (Type, Func<IViewModel>)>();

        public IViewModel CreateViewModel(string name)
        {
            if (ViewModelPageList.TryGetValue(name, out var valueTuple))
            {
                (_, Func<IViewModel> createViewModel) = valueTuple;
                return createViewModel?.Invoke();
            }

            return null;
        }

        public Type GetPageType(string name)
        {
            if (ViewModelPageList.TryGetValue(name, out var valueTuple))
            {
                (Type page, _) = valueTuple;
                return page;
            }

            return null;
        }
    }
}