namespace MyCSharpCode
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(nameof(AbcTaskAsync));
        }

        static async Task AbcTaskAsync()
        {
             int count = 200;
            var imp = new Imp(count);
            int[] indexs = Enumerable.Range(0, count).ToArray();
            await imp.ExecuteAsync(indexs, indexs, indexs);
            Console.WriteLine("完成");
        }
    }
}
