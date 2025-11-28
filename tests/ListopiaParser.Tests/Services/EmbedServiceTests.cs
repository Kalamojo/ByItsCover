using AwesomeAssertions;
using ListopiaParser.Configs;
using ListopiaParser.ResponseTypes;
using ListopiaParser.Services;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace ListopiaParser.Tests.Services;

public class EmbedServiceTests
{
    private IOptions<EmbedOptions> _options;
    private EmbedOptions _optionValues;
    private MockHttpMessageHandler _mockHttp;
    private EmbedService _sut;
    
    [SetUp]
    public void Setup()
    {
        _optionValues = new EmbedOptions
        {
            EmbedUrl = "http://127.0.0.1:8000/predict"
        };
        _options = Options.Create(_optionValues);
        _mockHttp = new MockHttpMessageHandler();
        
        var client = new HttpClient(_mockHttp);
        _sut =  new EmbedService(client, _options);
    }

    [Test]
    public async Task TestGetCoverEmbeddings()
    {
        var editionList = new List<Edition>
        {
            new()
            {
                Id = 1,
                Image = new EditionImage
                {
                    Url = "https://www.randomsite.com/test.png"
                },
                Isbn13 = "1111111111111"
            }
        };
        var expectedEmbeddings = new[] { 1.0f, 2f, 3.0f };
        var expectedCovers = new List<Cover>
        {
            new()
            {
                CoverId = 1,
                Isbn13 = "1111111111111",
                Url = "https://www.randomsite.com/test.png",
                Embedding = new ReadOnlyMemory<float>(expectedEmbeddings)
            }
        };
        var expectedRequest = _mockHttp.Expect(_optionValues.EmbedUrl)
            .WithJsonContent(new 
            {
                image_urls = new[] 
                { 
                    "https://www.randomsite.com/test.png" 
                }
            })
            .Respond("application/json", "{\"image_embeddings\":[[1.0, 2.0, 3.0]]}");

        var covers = await _sut.GetCoverEmbeddings(editionList, CancellationToken.None);
        var coversList = covers.ToList();
        
        Assert.That(_mockHttp.GetMatchCount(expectedRequest), Is.EqualTo(1));
        Assert.That(coversList, Is.Not.Null);
        Assert.That(coversList.Count,  Is.EqualTo(1));
        coversList.Should().BeEquivalentTo(expectedCovers, options => options
            .Using<ReadOnlyMemory<float>?>(ctx =>
            {
                if (ctx.Subject is null && ctx.Expectation is null)
                    return;
                ctx.Subject.Should().NotBeNull();
                ctx.Expectation.Should().NotBeNull();
                
                ctx.Subject.Value.Span.ToArray().Should().Equal(ctx.Expectation.Value.Span.ToArray());
            })
            .WhenTypeIs<ReadOnlyMemory<float>?>());
    }

    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
    }
}