using SQLite4Unity3d;

public class DB
{
	[PrimaryKey]
	public int id { get; set; }

	public DB() { }

	public new virtual string ToString()
	{
		return $"DB: id={id}";
	}
}