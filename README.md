# it.bz.noi.calendar-api

## Configuration

The following environment variables are needed:

```ini
TENANT_ID=b6acf547-c352-4708-a725-ec0e662c5d11
CLIENT_ID=f275e5cc-1c0c-4b1c-8308-4d645c13c39b
OPENID_AUTHORITY=https://auth.opendatahub.testingmachine.eu/auth/realms/noi/
USERNAME=noiappexchbutler@noi.bz.it
PASSWORD=<PASSWORD>
MEETING_ROOMS=meetingroom02@noi.bz.it,meetingroom41@noi.bz.it,meetingroom14@noi.bz.it,meetingroom26@noi.bz.it
NUMBER_OF_EVENTS=3
```

<dl>
  <dt>`MEETING_ROOMS`</dt>
  <dd>The meeting rooms to retrieve can be provided with the `MEETING_ROOMS` environment variable. You can delimit the rooms by comma `,` or by semicolon `;`.</dd>

  <dt>`NUMBER_OF_EVENTS` (optional, defaults to 5)</dt>
  <dd>The number of retrieved events can be configured by setting the `NUMBER_OF_EVENTS` environment variable.</dd>
</dl>
