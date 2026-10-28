CREATE TABLE IF NOT EXISTS authors (
    id      SERIAL PRIMARY KEY,
    name    VARCHAR(100) NOT NULL,
    country VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS books (
    id        SERIAL PRIMARY KEY,
    title     VARCHAR(200) NOT NULL,
    year      INT,
    author_id INT NOT NULL REFERENCES authors(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS genres (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS book_genres (
    book_id  INT NOT NULL REFERENCES books(id) ON DELETE CASCADE,
    genre_id INT NOT NULL REFERENCES genres(id) ON DELETE CASCADE,
    PRIMARY KEY (book_id, genre_id)
);

CREATE OR REPLACE FUNCTION get_books_by_author(p_author_id INT)
RETURNS TABLE (id INT, title VARCHAR(200), year INT, author_id INT)
LANGUAGE SQL
AS $$
    SELECT b.id, b.title, b.year, b.author_id
    FROM books b
    WHERE b.author_id = p_author_id
    ORDER BY b.id;
$$;
