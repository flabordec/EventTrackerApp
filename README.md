# Setup

To run the application you need a Postgres database.

1. Install a Postgres database
1. Create a new Postgres user
    ```sql
    CREATE USER event_tracker_user WITH ENCRYPTED PASSWORD '<pass here>';
    ```
1. Create a schema for your user 
    ```sql
    CREATE SCHEMA event_data AUTHORIZATION event_tracker_user;
    ```
