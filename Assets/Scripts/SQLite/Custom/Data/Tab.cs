using SQLite4Unity3d;

public class Tab : DB
{
    public string name { get; set; }

    public override string ToString()
    {
        return $"Point: id={id}, name={name}";
    }
}