using RepetitiveFileCleaner;

var cleaner = new RepetitiveCleaner("beta");
cleaner.SolidOperation();
cleaner.SoftOperation();

int width = 5;
int hight = 3;
for (int i = 0; i < hight; i++)
{
    for(int j=0; j< width; j++)
    {
        Console.Write('*');
    }
    Console.WriteLine(); 
}


