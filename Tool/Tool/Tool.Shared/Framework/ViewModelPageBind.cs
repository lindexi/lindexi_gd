using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tool.Shared.Model;

namespace Tool.Shared.Framework
{
    public class ViewModelPageBind
    {
        public ViewModelPageBind()
        {
            PageModelList = new ReadOnlyCollection<PageModel>(_pageModelList);
        }

        public ViewModelPageBind(IEnumerable<(PageModel name, Type page, Func<IViewModel> createViewModel)> list) : this()
        {
            RegisterPage(list);
        }

        public void RegisterPage(IEnumerable<(PageModel name, Type page, Func<IViewModel> createViewModel)> list)
        {
            foreach (var (name, page, createViewModel) in list)
            {
                RegisterPage(name, page, createViewModel);
            }
        }

        private void RegisterPage(PageModel name, Type page, Func<IViewModel> createViewModel)
        {
            ViewModelPageList.Add(name.Name, (page, createViewModel));

            _pageModelList.Add(name);
        }

        public IReadOnlyCollection<PageModel> PageModelList { get; }

        private readonly List<PageModel> _pageModelList = new List<PageModel>();

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