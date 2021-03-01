# ZaifAPI

BTC取引所ZaifのAPIをラップしてるだけのもの。

変なところが多いが、作ったのだいぶ前なので理由は全く覚えていない。

```
string key = [APIキー];
string secret = [シークレット];

var api = new ZaifAPI(key,secret);


var info = JsonConvert.DeserializeObject<ResultInfo>(await api.getInfo());
var jpy = info.result.deposit.jpy;  //"return"は被っているのでresultに置き換えている。
```


