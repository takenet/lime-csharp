# LIME command line interface


## Interactive (default)

```
lime --identity andreb@msging.net --password 123456 --instance home --uri net.tcp://tcp.msging.net:443 --presence.status available --presence.routingrule identity --verbose

Welcome to LIME
Successfully connected on server 'postmaster@msging.net/#az-iris1'
Send 'help' if you are in trouble
> send-text-message --to andreb@msging.net --type text/plain --content Oi
```

## Using custom connection and actions file

```
lime --connection connection.json --actions actions.json 
```



## Non-interactive

```
lime --identity andreb@msging.net --password 123456 --instance home --uri net.tcp://tcp.msging.net:443 --action send-text-message --action.to andreb@msging.net --action.type text/plain --action.content Oi
```
