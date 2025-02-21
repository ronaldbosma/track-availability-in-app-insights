output myenv string = substring(uniqueString('00000000-0000-0000-0000-000000000000', 'myenv', 'norwayeast'), 0, 5)

output test string = substring(uniqueString('00000000-0000-0000-0000-000000000000', 'testenvironment', 'norwayeast'), 0, 5)

output swedencentral string = substring(uniqueString('00000000-0000-0000-0000-000000000000', 'myenv', 'swedencentral'), 0, 5)
