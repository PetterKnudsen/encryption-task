﻿//
// Lag en applikasjon som krypterer hvert ord i teksten under, 5000 ganger.
// Velg krypteringsalgoritme sjølv.
// Samle de krypterte ordene i en array med index til ordets posisjon.
// Skriv arrayen til en fil.
//


using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const int encryptionIterations = 5000;

const string textToEncrypt = """

                             Examples

                             A Non-programming Example

                             Suppose a simple scheme for a repair centre, involving a manager and a group of technicians. The manager is responsible for receiving articles, and assigning an article to be repaired by a technician. All technicians have similar skills for repairing articles, and each one is responsible to repair one article at a time, independent of the other technicians. When a technician finishes repairing his assignment, he notifies the manager; the manager then assigns him a new article to be repaired, and so on. In general, repairing articles represents an irregular problem: some articles may present a simple fix and take a little amount of time, while others may require a more complex repair. Also, the effectiveness of this scheme relies on the fact that the number of articles that arrive to the centre can be substantially larger than the number of technicians available.

                             A Programming Example

                             Consider a real-time ultrasonic imaging system [GSVOM97], designed to acquire, process and display a tomographic image. Data is acquired based on the reflection of an ultrasonic signal that excites an array of 56 ceramic sensors. Data is amplified and digitalised to form a black and white image of 56´ 256 pixels, each one represented by a byte. An interpolation program is required to process the image, enlarging it to make it clearer to the observer. The image is displayed on a standard resolution monitor (640´ 480 pixels) in real-time, this is, at least 25 frames per second. In accordance with these requirements, an interpolation by a factor 3 between columns was chosen, enlarging the information of each image three times. A calculation shows the volume of data to be processes per second: each frame is represented as 168´ 256´ 1 bytes, and using 25 frames per second, makes a total of 1.075200 Mbytes per second. Using a manager-worker system for the cubic interpolation, the image is received a stream of pixels by the manager, which assigns to each worker a couple of pixels. Each worker uses each couple of pixels as input data, and calculates the cubic interpolation between them, producing other four interpolated pixels. As the number of workers is less than the total number of pixels, each worker requests for more work to the manager as soon as it finishes its process, and so on.

                             Problem

                             A computation is required where independent computations are performed, perhaps repeatedly, on all elements of some ordered data. Each computation can be performed completely, independent of the activity of other elements. Data is distributed among components without a specific order. However, an important feature is to preserve the order of data. Consider, for example, an imaging problem, where the image can be divided into smaller sub-images, and the computation on each sub-image does not require the exchange of messages between components. This can be carried out until completion, and then the partial results gathered. The

                             overall affect is to apply the computation to the whole image. If this computation is carried out serially, it should be executed as a sequence of serial jobs, applying the same computation to each sub-image one after another. Generally, performance as execution time is the feature of interest.

                             Forces

                             Using the previous problem description and other elements of parallel design, such as granularity and load balance [Fos94, CT92], the following forces are found:

                             Preserve the order of data. However, the specific order of data distribution and operation among processing elements is not important

                             The same computation can be performed independently and simultaneously on different pieces of data.

                             Data pieces may exhibit different sizes.

                             Changes in the number of processing elements should be reflected by the execution time.

                             Improvement in performance is achieved when execution time decreases.

                             """;


CompleteTask();

return;


void CompleteTask()
{
    // Using aes to encrypt
    using var aes = Aes.Create();
    aes.KeySize = 256;

    aes.GenerateKey();
    aes.GenerateIV();

    var iv = aes.IV;
    var key = aes.Key;

    // First, we create an array out of the words in the text
    var wordArray = GetWordArray();

    // Then, we encrypt the words
    // I'm not 100% sure what "Samle de krypterte ordene i en array med index til ordets posisjon." means.
    // For this task I've decided that it simply means that the encrypted word array matches up to the non encrypted word array.
    var encryptedWords = EncryptWordArray(wordArray.ToList(), encryptionIterations, iv, key);

    var encryptedWordList = encryptedWords.ToList();
    
    // Finally, we write the encrypted array to a text file
    WriteListToFile(encryptedWordList, "encryptedWords.txt");

    var decryptedWords = DecryptWordArray(encryptedWordList, encryptionIterations, iv, key);


    // As a double check - decrypt the array again to see if the decrypted text matches the original
    WriteListToFile(decryptedWords, "decrypted.txt");
}

IEnumerable<string> GetWordArray()
{
    var textWithNoNewLines = textToEncrypt.Replace("\n", "");
    // The task doesn't specify what a word is
    // So to make it simple, I've decided not to handle
    // Extra characters in words, like ',' and '.'
    // If you wanted to you could use regex to make the split smarter
    return textWithNoNewLines.Split(' ');
}

IEnumerable<string> EncryptWordArray(List<string> array, int iterations, byte[] iv, byte[] key)
{
    var encryptionResults = new ConcurrentBag<EncryptionResult>();

    // This takes a good while to complete
    Parallel.ForEach(array, (word, _, index) =>
    {
        var result = word;
        result = EncryptWord(result, iterations, iv, key);

        // Log so that progress is visible
        Console.WriteLine($"Done with {word}");
        encryptionResults.Add(new EncryptionResult
        {
            Index = index,
            Result = result
        });
    });

    // Write result so that it can be verified, encrypting 5000 times takes a long time.
    WriteEncryptionResultsToFile(encryptionResults);

    // Create an empty array we can use to store encrypted words in correct placement
    var results = Enumerable.Repeat(string.Empty, encryptionResults.Count).ToArray();
    foreach (var encryptionResult in encryptionResults)
    {
        results[encryptionResult.Index] = encryptionResult.Result;
    }

    return results;
}

static string EncryptWord(string word, int iterations, byte[] iv, byte[] key)
{
    using var aes = Aes.Create();
    aes.IV = iv;
    aes.Key = key;
    
    var encryptedBytes = Encoding.UTF8.GetBytes(word);

    for (var i = 0; i < iterations; i++)
    {
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(encryptedBytes, 0, encryptedBytes.Length);
            cs.Close();
        }

        encryptedBytes = ms.ToArray();
    }

    return Convert.ToBase64String(encryptedBytes);
}

void WriteListToFile(IEnumerable<string> wordList, string fileName)
{
    const string docPath = "./";
    using var outputFile = new StreamWriter(Path.Combine(docPath, fileName));
    // The task does not specify how it wants the array written to a file,
    // so I've decided to write every word back to a file on their own line
    foreach (var word in wordList)
    {
        // Write some empty lines above/below so that the result file has some "air" to it
        outputFile.WriteLine("");
        outputFile.WriteLine(word);
        outputFile.WriteLine("");
    }
}

// This function is simply so that the result can be verified
void WriteEncryptionResultsToFile(IEnumerable<EncryptionResult> encryptionResults)
{
    const string docPath = "./";
    using var outputFile = new StreamWriter(Path.Combine(docPath, "encryptionResults.txt"));

    foreach (var result in encryptionResults)
    {
        // Write some empty lines above/below so that the result file has some "air" to it
        outputFile.WriteLine("");
        outputFile.WriteLine(JsonSerializer.Serialize(result));
        outputFile.WriteLine("");
    }
}

IEnumerable<string> DecryptWordArray(List<string> array, int iterations,  byte[] iv, byte[] key)
{
    var decryptionResults = new ConcurrentBag<EncryptionResult>();

    // This takes a good while to complete
    Parallel.ForEach(array, (word, _, index) =>
    {
        var result = word;
        result = DecryptWord(result, iterations, iv, key);

        // Just reuse EncryptionResult class, this isnt part of the task anyway
        decryptionResults.Add(new EncryptionResult
        {
            Index = index,
            Result = result
        });
    });

    // Write result so that it can be verified, encrypting 5000 times takes a long time.
    WriteEncryptionResultsToFile(decryptionResults);

    // Create an empty array we can use to store encrypted words in correct placement
    var results = Enumerable.Repeat(string.Empty, decryptionResults.Count).ToArray();
    foreach (var encryptionResult in decryptionResults)
    {
        results[encryptionResult.Index] = encryptionResult.Result;
    }

    return results;
}

static string DecryptWord(string encryptedWord, int iterations,  byte[] iv, byte[] key)
{
    using var aes = Aes.Create();
    aes.IV = iv;
    aes.Key = key;
    
    var encryptedBytes = Convert.FromBase64String(encryptedWord);

    for (var i = 0; i < iterations; i++)
    {
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(encryptedBytes, 0, encryptedBytes.Length);
            cs.Close();
        }

        encryptedBytes = ms.ToArray();
    }

    return Encoding.UTF8.GetString(encryptedBytes);
}

internal class EncryptionResult
{
    public long Index { get; set; }
    public string Result { get; set; }
}