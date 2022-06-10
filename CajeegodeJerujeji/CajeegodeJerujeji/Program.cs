// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");


void CheckParentGroupExist(List<CoursewareListItem> allItems)
{
    var items = allItems
        .Where(i => !string.IsNullOrWhiteSpace(i.ParentId))
        .GroupBy(i => i.ParentId).ToList();
    var ghostParentItems = new List<CoursewareListItem>();
    foreach (var item in items)
    {
        if (allItems.Any(i => i.LocalId == item.Key))
        {
            continue;
        }
        ghostParentItems.AddRange(item.ToList());
    }
}

class CoursewareListItem
{
    public string LocalId { get; set; }
    public string ParentId { set; get; }
}