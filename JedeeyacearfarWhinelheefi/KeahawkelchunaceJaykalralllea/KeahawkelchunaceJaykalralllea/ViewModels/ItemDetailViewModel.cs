using System;

using KeahawkelchunaceJaykalralllea.Models;

namespace KeahawkelchunaceJaykalralllea.ViewModels
{
    public class ItemDetailViewModel : BaseViewModel
    {
        public Item Item { get; set; }
        public ItemDetailViewModel(Item item = null)
        {
            Title = item?.Text;
            Item = item;
        }
    }
}
