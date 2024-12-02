export default function Try() {
    const data = ['aaa', 'bbb', 'ccc', 'ddd']
    return (
        <>
            <ul>
                {data.map((d, i) =>
                    <li key={i}>{d}</li>)}
            </ul>
        </>
    )
}