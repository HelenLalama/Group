using System.Collections;
using System.Xml.Serialization;

namespace compression;

public class CompressedString : IEnumerable<char>
{
    private int maxMatchLength;
    private int windowSize;
    private DoubleEndedArrayQueue<char> encoded = new DoubleEndedArrayQueue<char>();
    private DoubleEndedArrayQueue<int> countsToTakeFromOriginal = new DoubleEndedArrayQueue<int>();
    private DoubleEndedArrayQueue<int> amountsToBacktrackInEncoded = new DoubleEndedArrayQueue<int>();
    private DoubleEndedArrayQueue<int> countsToCopyFromEncoded = new DoubleEndedArrayQueue<int>();

    public CompressedString(IEnumerable<char> original)
    {
        foreach (char c in original)
        {
            encoded.Add(c);
        }
        countsToTakeFromOriginal.Add(encoded.Size());
        amountsToBacktrackInEncoded.Add(0);
        countsToCopyFromEncoded.Add(0);
    }

    public static IEnumerable<T> Take<T>(IEnumerator<T> enumerator, int count)
    {
        for (int i = 0; i < count; i++)
        {
            enumerator.MoveNext();
            yield return enumerator.Current;
        }
    }

    public static IEnumerable<char> Decompress(IEnumerable<char> encoded, IEnumerable<int> take, IEnumerable<int> backtrack, IEnumerable<int> copy)
    {
        DoubleEndedArrayQueue<char> result = new DoubleEndedArrayQueue<char>();

        var orig = encoded.GetEnumerator();
        var backs = backtrack.GetEnumerator();
        var copies = copy.GetEnumerator();
        foreach (int numberToTake in take)
        {
            backs.MoveNext();
            copies.MoveNext();
            foreach (char c in Take(orig, numberToTake))
            {
                result.Add(c);
            }
            int startIndex = result.Size() - backs.Current;
            int copyCount = copies.Current;
            for (int i = 0; i < copyCount; i++)
            {
                result.Add(result.Get(startIndex + i));
            }
        }
        return result;
    }

    public void Compress(IEnumerable<char> original)
    {
        char currentChar = '\0';
        int count = 0;
        foreach (char c in original)
        {
            if (currentChar == '\0')
            {
                currentChar = c;
                count = 1;
            }
            else if (currentChar == c)
            {
                count++;
            }
            else
            {
                // Add the run-length encoded data to encoded
                encoded.Add(currentChar);
                encoded.Add((char)count);

                currentChar = c;
                count = 1;
            }
        }

        // Add the final run-length encoded data
        encoded.Add(currentChar);
        encoded.Add((char)count);

        countsToTakeFromOriginal.Add(original.Count());
        amountsToBacktrackInEncoded.Add(0);
        countsToCopyFromEncoded.Add(0);
    }

    public IEnumerator<char> GetEnumerator()
    {
        return Decompress(encoded, countsToTakeFromOriginal, amountsToBacktrackInEncoded, countsToCopyFromEncoded).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int EncodedLength()
    {
        return encoded.Size() + countsToCopyFromEncoded.Size();
    }
}