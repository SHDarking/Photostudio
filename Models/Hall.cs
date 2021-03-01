namespace Photostudio.Models
{
    public class Hall
    {
        public string IdHall { get; set;}
        public string Name { get; set; }
        public int Price { get; set; }
        public string Description { get;set; }

        public Hall()
        {
            
        }
        
        public Hall(string idHall, string name, int price, string description)
        {
            IdHall = idHall;
            Name = name;
            Price = price;
            Description = description;
        }

        public Hall(string idHall)
        {
            IdHall = idHall;
        }
    }
}