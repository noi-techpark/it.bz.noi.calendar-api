<!--
SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>

SPDX-License-Identifier: CC0-1.0
-->

# it.bz.noi.calendar-api

![REUSE Compliance](https://github.com/noi-techpark/it.bz.noi.calendar-api/actions/workflows/reuse.yml/badge.svg)

## Configuration

The following environment variables are needed:

```ini
TENANT_ID=b6acf547-c352-4708-a725-ec0e662c5d11
CLIENT_ID=f275e5cc-1c0c-4b1c-8308-4d645c13c39b
USERNAME=noiappexchbutler@noi.bz.it
PASSWORD=<PASSWORD>
OPENID_AUTHORITY=https://auth.opendatahub.testingmachine.eu/auth/realms/noi/
MEETING_ROOMS=meetingroom02@noi.bz.it,meetingroom41@noi.bz.it,meetingroom14@noi.bz.it,meetingroom26@noi.bz.it
NUMBER_OF_EVENTS=3
```

<dl>
  <dt>`TENANT_ID`</dt>
  <dd>The ID for the Azure Tenant to authenticate against Azure AD.</dd>

  <dt>`CLIENT_ID`</dt>
  <dd>The ID for the client app registered in Azure AD.</dd>

  <dt>`USERNAME`</dt>
  <dd>The username for the authentication againts Azure AD</dd>

  <dt>`PASSWORD`</dt>
  <dd>The password for the authentication againts Azure AD</dd>

  <dt>`OPENID_AUTHORITY`</dt>
  <dd>The auth endpoint for the Open ID provider (NOI Auth server in this case)</dd>

  <dt>`MEETING_ROOMS`</dt>
  <dd>The meeting rooms to retrieve can be provided with the `MEETING_ROOMS` environment variable. You can delimit the rooms by comma `,` or by semicolon `;`.</dd>

  <dt>`NUMBER_OF_EVENTS` (optional, defaults to 5)</dt>
  <dd>The number of retrieved events can be configured by setting the `NUMBER_OF_EVENTS` environment variable.</dd>
</dl>

## Usage

Authenticate with the NOI Authentication server.

You can use OData query functionality. The following OData query options are available: `$top`, `$filter`, `$select`, `$orderby`, `$count`

### Examples

* Query for the next 3 events:   
  `<BASE_URL>?$top=3&$orderby=start`

* Query for the events of a specific meeting room:   
  `<BASE_URL>?$filter=meetingroom eq 'meetingroom02@noi.bz.it'`

* Query for the events containing a specific subject:    
  `<BASE_URL>?$filter=contains(subject, 'Meeting')`

* Return only subject and start date:    
  `<BASE_URL>?$select=subject,start`

## REUSE

This project is [REUSE](https://reuse.software) compliant, more information about the usage of REUSE in NOI Techpark repositories can be found [here](https://github.com/noi-techpark/odh-docs/wiki/Guidelines-for-developers-and-licenses#guidelines-for-contributors-and-new-developers).

Since the CI for this project checks for REUSE compliance you might find it useful to use a pre-commit hook checking for REUSE compliance locally. The [pre-commit-config](.pre-commit-config.yaml) file in the repository root is already configured to check for REUSE compliance with help of the [pre-commit](https://pre-commit.com) tool.

Install the tool by running:
```bash
pip install pre-commit
```
Then install the pre-commit hook via the config file by running:
```bash
pre-commit install
```
